using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MIFCore.TestApp
{
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly SomeJob someJob;

        public TestController(SomeJob someJob)
        {
            this.someJob = someJob;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok("yep");
        }
    }
}
