using CityDataSource;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RedisList.Web.Models
{
    public class RedisHelper
    {
        public static IDatabase conn = null;
        public static void RedisConnectionAndUpload()
        {
            var muxer = ConnectionMultiplexer.Connect(configuration: "127.0.0.1:6379");
            
                conn = muxer.GetDatabase();
            muxer.Wait(conn.PingAsync());

            List<City> citys = CityDataSource.CityDataSource.GetCitys();

            if (conn.StringGet("IsValue").IsNull)
            {
                citys.ForEach(c =>
                {
                    conn.HashSetAsync("Citys:Data:" + c.Id.ToString(), c.ToHashEntries());

                    List<string> prefix = GetPrefix(c.Name);

                    prefix.Concat(GetPrefix(c.Code));

                    if (!string.IsNullOrEmpty(c.Name))
                        conn.SortedSetAdd("CityName", c.Name, 0);
                    if (!string.IsNullOrEmpty(c.Code))
                        conn.SortedSetAdd("CityCode", c.Code, 0);

                    foreach (var p in prefix)
                    {
                        conn.SortedSetAdd("Citys:index:" + p, c.Id, 0);
                    }
                });
                conn.StringSet("IsValue", true);
            }
        }

        public static List<City> GetCityByCode(string name)
        {
            RedisValue[] rvs = conn.SortedSetRangeByRank("Citys:index:" + name.ToLower());

            var citys = new List<City>();

            foreach (var r in rvs)
            {                
                HashEntry[] rvh = conn.HashGetAll("Citys:Data:" + r);
                citys.Add(RedisUtils.ConvertFromRedis<City>(rvh));
            }
            return citys;
        }

        public static List<string> GetPrefix(string word)
        {

            if (string.IsNullOrEmpty(word))
                return new List<string>();

            word = word.ToLower();
            var hs = new List<string>();

            string[] wordsSplit = word.Split(separator: new char[] { ' ' });

            foreach (var w in wordsSplit)
            {
                int i = 2;
                for (; i <= w.Length;)
                {
                    hs.Add(w.Substring(0, i++));
                }

            }

            return hs;
        }
    }

   
    public static class RedisUtils
    {
        //Serialize in Redis format:
        public static HashEntry[] ToHashEntries(this object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();
            return properties.Where(property=> IsPrimitive(property.PropertyType))
                .Select(property => new HashEntry(property.Name, property.GetValue(obj) == null ? string.Empty : property.GetValue(obj).ToString())).ToArray();
        }
        //Deserialize from Redis format
        public static T ConvertFromRedis<T>(this HashEntry[] hashEntries)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            object obj = Activator.CreateInstance(typeof(T));
            foreach (var property in properties)
            {
                Type t = property.PropertyType;
                if (IsPrimitive(t))
                {
                    HashEntry entry = hashEntries.FirstOrDefault(g => g.Name.ToString().Equals(property.Name));
                    if (entry.Equals(new HashEntry())) continue;
                    if (entry.Value.HasValue && !string.IsNullOrEmpty(entry.Value.ToString()))
                    {
                        property.SetValue(obj, Convert.ChangeType(entry.Value.ToString(), property.PropertyType));
                    }
                }
            }
            return (T)obj;
        }

        private static bool IsPrimitive(Type t)
        {
            return t.IsValueType || t.IsPrimitive || t == typeof(string);
        }
    }
}
