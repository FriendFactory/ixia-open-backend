using System.Collections;
using System.Reflection;
using Common.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.Common.IntegrationTesting;

public partial class DataEnvironment
{
    public string ResolveSqlText(string scriptName)
    {
        var scriptFile = LookupSqlScriptByNamePart(scriptName);
        var scriptText = SqlScrips[scriptFile];
        return scriptText;
    }

    /// <summary>
    ///     Executes commands from the script (usually script should contains INSERT INTO ... VALUES (), ()... RETURNING *;)
    ///     and returns output (which is usually the full table, specified by `RETURNING *` part of script)
    ///     as entity collection.
    ///
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    /// 
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    /// 
    ///     Property names will be converted to camel case due parameter substitution.
    /// 
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task<TEntity[]> WithData<TEntity>(string scriptName, object parameters)
    {
        var scriptFile = LookupSqlScriptByNamePart(scriptName);
        var scriptText = SqlScrips[scriptFile];

        var parameterDictionary = ExtractParameters(parameters);
        var parameterizedScriptText = ParameterizeSql(scriptText, parameterDictionary);

        var result = await Db.Database.SqlQueryRaw<TEntity>(parameterizedScriptText).ToArrayAsync();
        return result;
    }


    /// <summary>
    ///     Executes commands from the script which ignoring any output.
    /// 
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    ///     Property names will be converted to camel case due parameter substitution.
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task WithScript(string scriptName, object parameters = null)
    {
        var scriptFile = LookupSqlScriptByNamePart(scriptName);
        var scriptText = SqlScrips[scriptFile];

        var parameterDictionary = ExtractParameters(parameters);
        var parameterizedScriptText = ParameterizeSql(scriptText, parameterDictionary);

        await Db.Database.ExecuteSqlRawAsync(parameterizedScriptText);
    }

    /// <summary>
    ///     Executes commands from the script which ignoring any output for every element in <paramref name="parameters" />.
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    ///     Property names will be converted to camel case due parameter substitution.
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task WithScriptForEach(string scriptName, IEnumerable<object> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        foreach (var item in parameters)
        {
            ArgumentNullException.ThrowIfNull(item);
            await WithScript(scriptName, item);
        }
    }

    /// <summary>
    ///     Executes commands from the script (usually script should contains INSERT INTO ... VALUES (), ()... RETURNING *;)
    ///     and returns output (which is usually the full table, specified by `RETURNING *` part of script)
    ///     as entity collection.
    ///
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    /// 
    ///     Entity should be one of entities registered in db context.
    /// 
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    /// 
    ///     Property names will be converted to camel case due parameter substitution.
    /// 
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task<TEntity[]> WithEntity<TEntity>(string scriptName, object parameters)
        where TEntity : class
    {
        var scriptFile = LookupSqlScriptByNamePart(scriptName);
        var scriptText = SqlScrips[scriptFile];

        var parameterDictionary = ExtractParameters(parameters);
        var parameterizedScriptText = ParameterizeSql(scriptText, parameterDictionary);

        var result = await Db.Set<TEntity>().FromSqlRaw(parameterizedScriptText).ToArrayAsync();
        return result;
    }

    /// <summary>
    ///     Executes commands from the script (usually script should contains INSERT INTO ... VALUES (), ()... RETURNING *;)
    ///     and returns output (which is usually the full table, specified by `RETURNING *` part of script)
    ///     for each entry of <paramref name="parameters" /> array and return flattened output as entity collection.
    ///
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    /// 
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    /// 
    ///     Property names will be converted to camel case due parameter substitution.
    /// 
    ///     Unlike the <see cref="WithData{TEntity}" /> method, this method would not execute a script
    ///     and returns an empty collection if empty collection is passed as argument.
    /// 
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task<TEntity[]> WithDataCollection<TEntity>(string scriptName, IEnumerable<object> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var result = new List<TEntity>();

        foreach (var item in parameters)
        {
            var res = await WithData<TEntity>(scriptName, item);
            result.AddRange(res);
        }

        return result.ToArray();
    }


    /// <summary>
    ///     Executes commands from the script (usually script should contains INSERT INTO ... VALUES (), ()... RETURNING *;)
    ///     and returns output (which is usually the full table, specified by `RETURNING *` part of script)
    ///     for each entry of <paramref name="parameters" /> array and return flattened output as entity collection.
    ///
    ///     Running this method opens a transaction, which will be rolled back on disposing DataEnvironment instance.
    /// 
    ///     Entity should be registered in db context.
    /// 
    ///     Script can contain parameters (using :paramName) syntax, actual values will be substituted
    ///     using property name and property values of <paramref name="parameters" /> object.
    /// 
    ///     Property names will be converted to camel case due parameter substitution.
    /// 
    ///     Unlike the <see cref="WithEntity{TEntity}" /> method, this method would not execute a script
    ///     and returns an empty collection if empty collection is passed as argument.
    /// 
    ///     DO NOT USE this code outside of test project, as it is subject of SQL injections vulnerability.
    /// </summary>
    public async Task<TEntity[]> WithEntityCollection<TEntity>(string scriptName, IEnumerable<object> parameters)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var result = new List<TEntity>();

        foreach (var item in parameters)
        {
            var res = await WithEntity<TEntity>(scriptName, item);
            result.AddRange(res);
        }

        return result.ToArray();
    }

    /// <summary>
    ///     Applies changes from script from the file.
    ///
    ///     Script doesn't start a transaction
    ///     so changes applied by script might be permanent if transaction is not open yet.
    ///
    ///     Transaction might be open implicitly, for example by calling WithXXX method,
    ///     in this case the script will be run inside the transaction.
    ///     
    ///     Script should allow repeatable runs.
    /// </summary>
    public async Task ApplyScript(string scriptName)
    {
        var fileName = LookupSqlScriptByNamePart(scriptName);
        var text = SqlScrips[fileName];

        await Db.Database.ExecuteSqlRawAsync(text);
    }

    private static string ParameterizeSql(string sql, Dictionary<string, string> parameters)
    {
        var result = sql;
        foreach (var (key, value) in parameters)
            result = result.Replace($":{key}", value);

        return result;
    }

    private static Dictionary<string, string> ExtractParameters(object parameters)
    {
        if (parameters == null)
            return new Dictionary<string, string>();

        if (parameters is IDictionary dict)
        {
            var typedDictionary = new Dictionary<string, string>();
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value is null)
                    typedDictionary[entry.Key.ToString()] = null;
                else if (entry.Value is string str)
                    typedDictionary[entry.Key.ToString()] = str;
                else
                    typedDictionary[entry.Key.ToString()] = entry.Value.ToString();

                return typedDictionary;
            }
        }

        var result = new Dictionary<string, string>();
        foreach (var prop in parameters.GetType()
                                       .GetProperties(
                                            BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public |
                                            BindingFlags.FlattenHierarchy
                                        ))
            if (prop.IsDefined(typeof(SqlParamFlattenNestedAttribute), true))
            {
                var value = prop.GetValue(parameters);
                if (value is not null)
                {
                    var nested = ExtractParameters(value);
                    foreach (var (key, val) in nested)
                        result[key] = val;
                }
            }
            else
            {
                result[prop.Name.ToCamelCase()] = QuoteValue(prop, prop.GetValue(parameters));
            }

        return result;
    }


    private static string QuoteValue(PropertyInfo prop, object value)
    {
        if (value is null)
            return "null";

        if (value is string str)
            return $"'{str}'";

        if (value is Enum)
            return $"'{value}'";

        {
            if (value is string[] arr)
                return $"ARRAY[{
                    string.Join(",", arr.Select(v => $"'{v}'"))
                }]::text[]";
        }

        {
            if (value is long[] arr)
                return $"ARRAY[{
                    string.Join(",", arr.Select(v => v.ToString()))
                }]::bigint[]";
        }
        {
            if (value is long?[] arr)
                return $"ARRAY[{
                    string.Join(",", arr.Select(v => v.ToString()))
                }]::bigint[]";
        }

        if (value is DateTime dt)
            return $"'{dt.ToString("O")}'";

        if (prop.IsDefined(typeof(SqlParamJsonAttribute), true))
            return $"'{JsonConvert.SerializeObject(value)}'";

        return value.ToString();
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class SqlParamJsonAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class SqlParamFlattenNestedAttribute : Attribute { }