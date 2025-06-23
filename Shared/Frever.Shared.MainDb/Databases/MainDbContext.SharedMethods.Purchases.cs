using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Frever.Shared.MainDb;

public partial class MainDbContext
{
    public IQueryable<BalanceInfo> GetGroupBalanceInfo(long[] groupIds)
    {
        // This query uses registry-like approach to calculate balance
        // It declares three registers (daily, subscription and permanent) and
        // calculates next value for each store transaction using previous values and transaction data
        //
        // To get access to previously calculated registry data it utilizes recursive CTE:
        // - First we introduce new column with pass-through numeration (using row_number) which correctly order transactions
        // - Then recursive CTE is built:
        //    - start of recursion initializes registries to zeroes
        //    - next step of recursion selects a single next transaction based on row number (using rn + 1)
        //    - registry values are modified using business rules (see below)
        // This technique allows to do complicated rolling calculation
        //
        // Rules are next:
        // - Daily tokens used first
        // - Next subscription token are used
        // - Next permanent tokens are used

        var groupIdCondition = String.Join(", ", groupIds.Select(id => id.ToString()));

        var sql = $"""
                   with recursive
                       tr as (select "Id",
                                     "GroupId",
                                     "CreatedTime",
                                     "TransactionType",
                                     "HardCurrencyAmount",
                                     row_number() over (partition by "GroupId" order by "Id") as rn
                              from "AssetStoreTransaction"),
                       all_groups as (select "Id" as group_id
                                      from "Group"
                                      where "DeletedAt" is null
                                        and "IsBlocked" = false
                                        and "Id" in ({groupIdCondition})
                                        ),
                       reg as (select 0::bigint as rn,
                                      group_id,
                                      0::bigint as transaction_id,
                                      0         as permanent,
                                      0         as subscription,
                                      0         as daily
                               from all_groups
                               union all
                               select tr.rn,
                                      tr."GroupId" as group_id,
                                      tr."Id"      as transaction_id,
                                      case
                                          when tr."HardCurrencyAmount" < 0 then
                                              least(
                                                      reg.permanent,
                                                      reg.permanent + reg.subscription + reg.daily + tr."HardCurrencyAmount"
                                              )
                                          when tr."TransactionType" not in
                                               ('DailyTokenRefill', 'DailyTokenBurnout', 'MonthlyTokenRefill', 'MonthlyTokenBurnout')
                                              then reg.permanent + tr."HardCurrencyAmount"
                                          else reg.permanent
                                          end
                                                   as permanent,
                                      greatest(0,
                                               case
                                                   when tr."TransactionType" in ('MonthlyTokenRefill', 'MonthlyTokenBurnout')
                                                       then reg.subscription + tr."HardCurrencyAmount"
                                                   when tr."HardCurrencyAmount" < 0
                                                       then reg.subscription + least(0, reg.daily + tr."HardCurrencyAmount")
                                                   else reg.subscription
                                                   end
                                      )            as subscription
                                       ,
                                      case
                                          when tr."TransactionType" in ('DailyTokenRefill', 'DailyTokenBurnout')
                                              then reg.daily + "HardCurrencyAmount"
                                          when tr."TransactionType" in ('MonthlyTokenRefill', 'MonthlyTokenBurnout') then reg.daily
                                          when tr."HardCurrencyAmount" < 0 then greatest(0, reg.daily + tr."HardCurrencyAmount")
                                          else reg.daily
                                          end
                                                   as daily
                               from tr
                                        inner join reg on tr."GroupId" = reg.group_id and tr.rn = reg.rn + 1),
                       reg_ranked as (select group_id,
                                             transaction_id,
                                             permanent,
                                             subscription,
                                             daily,
                                             row_number() over (partition by group_id order by transaction_id desc) as rn
                                      from reg)

                   select r.group_id     as "GroupId",
                          r.permanent    as "PermanentTokens",
                          r.subscription as "SubscriptionTokens",
                          r.daily        as "DailyTokens"
                   from reg_ranked as r
                   where r.rn = 1
                   """;

        return Database.SqlQueryRaw<BalanceInfo>(sql);
    }

    public IQueryable<GroupActiveSubscriptionInfo> GetGroupActiveSubscriptions(bool excludeRefilledDailyTokens)
    {
        var notRefilledTodayFilter = """
                                     where not exists (select 1
                                     from "AssetStoreTransaction" tr
                                     where tr."GroupId" = grp."Id"
                                       and tr."TransactionType" = 'DailyTokenRefill'
                                       and tr."CreatedTime"::date = current_timestamp::date)
                                     """;

        var filter = excludeRefilledDailyTokens ? notRefilledTodayFilter : "";

        var sql = $"""
                   with grp as (select *
                                from "Group" as g
                                where g."IsBlocked" = false
                                  and g."DeletedAt" is null),
                        sub as (select *,
                                       row_number() over ( partition by s."GroupId" order by s."Id" desc) rn
                                from "InAppUserSubscription" s
                                where s."StartedAt"::date < current_timestamp::date
                                  and (s."CompletedAt" is null or s."CompletedAt"::date >= current_timestamp::date)),
                        default_sub as
                            (select *
                             from "InAppProduct" p
                             where p."IsSubscription"
                               and p."IsFreeProduct"
                               and p."IsActive"
                             order by p."Id" desc
                             limit 1),
                         res as (
                               select grp."Id"                                                               as "GroupId",
                                      sub."Id"                                                               as "SubscriptionId",
                                      sub."DailyHardCurrency"                                                as "ActiveSubscriptionDailyTokens",
                                      coalesce(sub."DailyHardCurrency", default_sub."DailyHardCurrency", 30) as "DailyTokens"
                               from grp
                                        left join
                                    sub on grp."Id" = sub."GroupId" and sub.rn = 1
                                        left join default_sub on 1 = 1
                               {filter}
                         )
                         select * from res
                   """;

        return Database.SqlQueryRaw<GroupActiveSubscriptionInfo>(sql);
    }
}

public class BalanceInfo
{
    public long GroupId { get; set; }
    public int PermanentTokens { get; set; }
    public int SubscriptionTokens { get; set; }
    public int DailyTokens { get; set; }
}

public class GroupActiveSubscriptionInfo
{
    public long GroupId { get; set; }
    public long? SubscriptionId { get; set; }
    public int? ActiveSubscriptionDailyTokens { get; set; }
    public int DailyTokens { get; set; }
}