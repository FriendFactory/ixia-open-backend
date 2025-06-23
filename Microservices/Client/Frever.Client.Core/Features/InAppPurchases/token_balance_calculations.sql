begin;


insert into "AssetStoreTransaction"("CreatedTime", "GroupId", "TransactionType", "HardCurrencyAmount",
                                    "SoftCurrencyAmount")
values ('2025-01-01 10:00:00', 1, 'InAppPurchase', 1000, 0),
       -- ^^ IA: D = 0     S = 0     P = 1000       T = 1000
       ('2025-01-02 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-02 11:15:00', 1, 'AiWorkflowRun', -100, 0),
       -- ^^ D0: D = 0     S = 0     P = 930       T = 930
       ('2025-01-03 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-03 11:10:00', 1, 'AiWorkflowRun', -10, 0),
       -- ^^ D1: D = 20     S = 0     P = 930       T = 950
       ('2025-01-04 11:00:00', 1, 'DailyTokenBurnout', -20, 0),
       ('2025-01-04 11:00:01', 1, 'DailyTokenRefill', 30, 0),
       -- ^^ D2: D = 30     S = 0     P = 930       T = 960
       ('2025-01-05 11:00:00', 1, 'AiWorkflowRun', -100, 0),
       -- ^^ D3: D = 0     S = 0     P = 860       T = 860
       ('2025-01-06 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-06 11:10:00', 1, 'AiWorkflowRun', -15, 0),
       -- ^^ D4: D = 15     S = 0     P = 860       T = 875
       ('2025-01-07 11:00:00', 1, 'DailyTokenBurnout', -15, 0),
       ('2025-01-07 11:00:01', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-07 11:30:00', 1, 'MonthlyTokenRefill', 1500, 0),
       -- ^^ D5: D = 30     S = 1500     P = 860       T = 2390
       ('2025-01-08 11:00:00', 1, 'AiWorkflowRun', -10, 0),
       -- ^^ D6: D = 20     S = 1500     P = 860       T = 2380
       ('2025-01-09 11:00:00', 1, 'DailyTokenBurnout', -20, 0),
       ('2025-01-09 11:00:01', 1, 'DailyTokenRefill', 30, 0),
       -- ^^ D7: D = 30     S = 1500     P = 860       T = 2390
       ('2025-01-10 11:00:00', 1, 'AiWorkflowRun', -1700, 0),
       -- ^^ D8: D = 0     S = 0     P = 690       T = 690
       ('2025-01-11 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       -- ^^ D9: D = 30     S = 0     P = 690       T = 720
       ('2025-01-12 11:00:00', 1, 'AiWorkflowRun', -20, 0),
       -- ^^ D10: D = 10     S = 0     P = 690       T = 700
       ('2025-01-13 11:00:00', 1, 'DailyTokenBurnout', -10, 0),
       ('2025-01-13 11:00:01', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-13 11:00:01', 1, 'InAppPurchase', 500, 0),
       -- ^^ D11: D = 30     S = 0     P = 1190         T = 1120
       ('2025-01-14 11:00:00', 1, 'MonthlyTokenRefill', 1500, 0),
       ('2025-01-14 11:10:00', 1, 'AiWorkflowRun', -100, 0),
       -- ^^ D12: D = 0     S = 1430     P = 1190       T = 2620
       ('2025-01-15 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       -- ^^ D13: D = 30     S = 1430     P = 1190      T = 2620
       ('2025-01-16 11:00:00', 1, 'MonthlyTokenBurnout', -1430, 0),
       -- CHECKPOINT: Here Balance = Permanent + Daily
       ('2025-01-16 11:00:01', 1, 'MonthlyTokenRefill', 1500, 0),
       -- ^^ D14: D = 30     S = 1500     P = 1190      T = 2720
       ('2025-01-17 11:00:00', 1, 'AiWorkflowRun', -100, 0),
       -- ^^ D15: D = 0     S = 1430     P = 1190       T = 2620
       ('2025-01-18 11:00:00', 1, 'DailyTokenRefill', 30, 0),
       ('2025-01-18 11:00:00', 1, 'AiWorkflowRun', -10, 0)
--------- ^^ D16: D = 20     S = 1430     P = 1190      T = 2640
;

with recursive
    tr as (select "Id",
                  "CreatedTime",
                  "TransactionType",
                  "HardCurrencyAmount",
                  row_number() over (order by "Id") as rn
           from "AssetStoreTransaction"
           where "GroupId" = 1),
    reg as (select 0::bigint    as rn,
                   null::bigint as "Id",
                   0            as permanent,
                   0            as subscription,
                   0            as daily
            union all
            select tr.rn,
                   tr."Id",
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
                   )   as subscription
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
                     inner join reg on tr.rn = reg.rn + 1)

select reg.permanent as "PermanentTokens",
       reg.subscription as "SubscriptionTokens",
       reg.daily as "DailyTokens",
       tr."HardCurrencyAmount", tr."TransactionType", tr."CreatedTime"
from reg
         inner join tr on reg."Id" = tr."Id"
order by tr."Id" desc
limit 1;

rollback;