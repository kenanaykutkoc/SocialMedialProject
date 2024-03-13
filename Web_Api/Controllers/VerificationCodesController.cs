﻿using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificationCodesController : ControllerBase
    {
        IVerificationCodeService _verificationCodeService;

        public VerificationCodesController(IVerificationCodeService verificationCodeService)
        {
            _verificationCodeService = verificationCodeService;
        }

        [HttpPost("sendcode")]
        public IActionResult SendVerificationCode(VerificationCodeDto verificationCode)
        {
            var result = _verificationCodeService.SendVerificationCode(verificationCode);

            return result.Success ? Ok(result) : BadRequest(result);
        }


        [HttpPost("deleteverifycode")]
        public IActionResult DeleteVerifyCode(int userId)
        {
            var result = _verificationCodeService.DeleteVerifyCode(userId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("checkverifycode")]
        public IActionResult CheckVerfiyCode(int userId, string code)
        {
            var result = _verificationCodeService.CheckVerifyCode(userId, code);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
