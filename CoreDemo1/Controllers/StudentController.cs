using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreDemo1.AuthHelper.OverWrite;
using CoreModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreDemo1.Controllers
{
    /// <summary>
    /// 学生类
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        /// <summary>
        /// 提交学生信息
        /// </summary>
        /// <param name="student"></param>
        /// <returns></returns>
        [HttpPost]
        //[ApiExplorerSettings(IgnoreApi = true)] //用于隐藏接口
        public ActionResult SetStident(StudentModel student)
        {
            return null;
        }

        public async Task<object> GetJwtStr(string name, string pass)
        {
            TokenModelJwt tokenModel = new TokenModelJwt { UId = 1, Role = "Admin" };
            var jwtStr = JwtHelper.IssueJwt(tokenModel);//登录,获取token令牌
            return new { token = jwtStr };
        }
    }
}