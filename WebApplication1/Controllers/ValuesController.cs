using ServiceLogger;
using System.Collections.Generic;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        // GET api/values
        [ApiLogger(Database = true, TextFile = true, EventViewer = true)]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [ApiLogger(Database = true, TextFile = true, EventViewer = true)]
        public string Get( int id )
        {
            return "value";
        }

        [Route("{id}/rh/{id2}/{id3}")]
        [ApiLogger(Database = true, TextFile = true, EventViewer = true)]
        public string Get( int id, string id2, string id3 )
        {
            return "value";
        }

        // POST api/values
        public void Post( [FromBody]string value )
        {
        }

        [Route("{id}/rh/{id2}/{id3}")]
        [ApiLogger(Database = true, TextFile = true, EventViewer = true)]
        public void Post( int id, string id2, string id3 )
        {
        }

        [Route("{id}/rh/{id2}/{id3}")]
        public void Put( int id, [FromBody]string value )
        {
        }

        // DELETE api/values/5
        public void Delete( int id )
        {
        }
    }
}