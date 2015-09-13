using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CityDataSource;
using RedisList.Web.Models;

namespace RedisList.Web.Controllers
{
    public class RedisController : Controller
    {
        // GET: Redis
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ValueMapper(int[] values)
        {
            var indices = new List<int>();

            if (values != null && values.Any())
            {
                var index = 0;
                foreach (var city in CityDataSource.CityDataSource.GetCitys())
                {
                    if (values.Contains((int)city.Id))
                    {
                        indices.Add(index);
                    }

                    index += 1;
                }
            }

            return this.Jsonp(indices);
        }

        [HttpGet]
        public ActionResult GetCity()
        {
            var take = string.IsNullOrEmpty(Request.QueryString["take"]) ? 1000 : Convert.ToInt32(Request.QueryString["take"]);
            var skip = string.IsNullOrEmpty(Request.QueryString["skip"]) ? 1000 : Convert.ToInt32(Request.QueryString["skip"]);
            
            var city = RedisHelper.GetCityByCode("ZA").Skip(skip).Take(take);
            
            return this.Json(city, JsonRequestBehavior.AllowGet);
        }
    }
}