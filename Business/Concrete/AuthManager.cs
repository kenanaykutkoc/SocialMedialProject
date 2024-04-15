﻿using Business.Abstract;
using Business.Constants;
using Business.ValidationRules.FluentValidation;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingconcerns.Logging.Log4Net.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Business;
using Core.Utilities.Result.Abstract;
using Core.Utilities.Result.Concrete;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using Entities.DTOs;
using Entities.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class AuthManager : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenHelper _tokenHelper;
        private readonly IUserDal _userDal;
        private readonly IUserOperationClaimDal _userOperationClaimDal;
        private readonly IUserImageDal _userImageDal;

        public AuthManager(IUserService userService, ITokenHelper tokenHelper, IUserDal userDal, IUserOperationClaimDal userOperationClaimDal, IUserImageDal userImageDal)
        {
            _userService = userService;
            _tokenHelper = tokenHelper;
            _userDal = userDal;
            _userOperationClaimDal = userOperationClaimDal;
            _userImageDal = userImageDal;
        }

        // [ValidationAspect(typeof(UserForRegisterDtoValidator))]
        //[LogAspect(typeof(FileLogger))]
        public IDataResult<User> Register(UserForRegisterDto userForRegisterDto, string password)
        {
            byte[] passwordHash, passwordSalt;
            HashingHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);
            var user = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Gender=userForRegisterDto.Gender,
                PhoneNumber=userForRegisterDto.PhoneNumber,
                Status = true
            };
            _userService.Add(user);

            var result = _userDal.Get(x=>x.Email==user.Email);
            var resultClaim = _userDal.GetClaims(user);
            var resultImage = _userImageDal.Get(x=>x.UserId==result.Id);
            if (result != null && resultClaim  != null && resultImage !=null)
            {
                return new ErrorDataResult<User>(Messages.UserNotFound);
            }

            var userOperationClaim = new UserOperationClaim
            {
                UserId = result.Id,
                OperationClaimId = 2
            };

            UserImage userImage = new UserImage
            {
                ImagePath = "images/default.jpg",
                UserId = result.Id,
                Date = DateTime.Now
            };

            _userImageDal.Add(userImage);
            _userOperationClaimDal.Add(userOperationClaim);


            return new SuccessDataResult<User>(user, Messages.UserRegistered);
        }

        [ValidationAspect(typeof(UserForLoginDtoValidator))]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<User>> Login(UserForLoginDto userForLoginDto)
        {
            var userToCheck = await _userService.GetUserByMail(userForLoginDto.Email);
            if (userToCheck.Data == null)
            {
                return new ErrorDataResult<User>(Messages.UserNotFound);
            }

            if (!HashingHelper.VerifyPasswordHash(userForLoginDto.Password, userToCheck.Data.PasswordHash, userToCheck.Data.PasswordSalt))
            {
                return new ErrorDataResult<User>(Messages.PasswordError);
            }

            return new SuccessDataResult<User>(userToCheck.Data, Messages.SuccessfulLogin);
        }
        //
        public async Task <IResult> UserExists(string email)
        {
            var check = await _userService.GetUserByMail(email);
            if (check.Data != null)
            {
                return new ErrorResult(Messages.UserAlreadyExists);
            }
            return new SuccessResult();
        }

        public IDataResult<AccessToken> CreateAccessToken(User user)
        {
            var claims = _userService.GetClaims(user);
            var accessToken = _tokenHelper.CreateToken(user, claims.Data);
            return new SuccessDataResult<AccessToken>(accessToken, user.FirstName);
        }

        //  [ValidationAspect(typeof(ChangePasswordValidator))]
        public async Task< IResult> ChangePassword(ChangePasswordModel updatedUser)
        {
            UserForLoginDto checkedUser = new UserForLoginDto
            {
                Email = updatedUser.Email,
                Password = updatedUser.OldPassword
            };
            var loginResult =await Login(checkedUser);
            if (loginResult.Success)
            {
                var user = loginResult.Data;
                byte[] passwordHash, passwordSalt;
                HashingHelper.CreatePasswordHash(updatedUser.NewPassword, out passwordHash, out passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                _userService.Update(user);
                return new SuccessResult(Messages.PasswordChanged);
            }

            return new ErrorResult(loginResult.Message);
        }

        public async Task<IResult> AdminChangePassword(string email, string newPassword)
        {
            var user = _userService.GetUserByMail(email);
            
            if (user !=null)
            {
                var data = user.Result.Data;
                
                byte[] passwordHash, passwordSalt;
                HashingHelper.CreatePasswordHash(newPassword, out passwordHash, out passwordSalt);
                data.PasswordHash = passwordHash;
                data.PasswordSalt = passwordSalt;
                _userService.Update(data);
                return new SuccessResult(Messages.PasswordChanged);
            }

            return new ErrorResult(Messages.UserNotFound);
        }

        //BUSSINESS RULES   
    }
}
