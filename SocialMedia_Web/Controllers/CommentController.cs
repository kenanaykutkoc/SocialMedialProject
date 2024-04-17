﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SocialMedia_Web.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SocialMedia_Web.Controllers
{
    public class CommentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Authorize(Roles = "admin,user")]
        [HttpPost("post-comment")]
        public async Task<IActionResult> Comment(Comment comment)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("Token");
            var jsonComment = JsonConvert.SerializeObject(comment);
            var content = new StringContent(jsonComment, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responseMessage = await httpClient.PostAsync("http://localhost:65527/api/Comments/add", content);
            if (responseMessage.IsSuccessStatusCode)
            {
                var successComment = await GetCommentResponseMessage(responseMessage);
                TempData["Message"] = successComment.Message;
                TempData["Success"] = successComment.Success;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var failComment = await GetCommentResponseMessage(responseMessage);
                TempData["Message"] = failComment.Message;
                TempData["Success"] = failComment.Success;
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(Roles = "admin,user")]
        [HttpPost("delete-comment")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("Token");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responseMessage = await httpClient.DeleteAsync("http://localhost:65527/api/Comments/delete?id=" + id);
            if (responseMessage.IsSuccessStatusCode)
            {
                TempData["Message"] = "Yorum Silindi";
                TempData["Success"] = true;
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "admin,user")]
        [HttpGet("notifications")]
        public async Task<IActionResult> Notifications()
        {
            var httpClient = _httpClientFactory.CreateClient();
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            ViewData["MyArticle"] = HttpContext.Session.GetInt32("MyArticle");
            ViewData["UserId"] = userId;
            var responseMessage = await httpClient.GetAsync("http://localhost:65527/api/Articles/getarticlewithdetailsbyuserid?id=" + userId);
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();
                var apiDataResponseMyArticle = JsonConvert.DeserializeObject<ApiListDataResponse<ArticleDetailDto>>(jsonResponse);

                return apiDataResponseMyArticle.Success ? View(apiDataResponseMyArticle.Data) : RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "admin,user")]
        public async Task<IActionResult> ReadAllNotifications()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var responseMessage = await _httpClientFactory.CreateClient().GetAsync("http://localhost:65527/api/Articles/getarticlewithdetailsbyuserid?id=" + userId);
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();
                var apiDataResponseMyArticle = JsonConvert.DeserializeObject<ApiListDataResponse<ArticleDetailDto>>(jsonResponse);

                var comments = apiDataResponseMyArticle.Data.SelectMany(x => x.CommentDetails);

                foreach (var item in comments)
                {
                    if (item.Status == false)
                    {
                        CommentDetail commentDetail = new CommentDetail()
                        {
                            Id = item.Id,
                            ArticleId = item.ArticleId,
                            UserId = item.UserId,
                            CommentText = item.CommentText,
                            CommentDate = item.CommentDate,
                            Status = true
                        };

                        var jsonComment = JsonConvert.SerializeObject(commentDetail);
                        var content = new StringContent(jsonComment, Encoding.UTF8, "application/json");
                        var responseReadAllNotifications = await _httpClientFactory.CreateClient().PutAsync("http://localhost:65527/api/Comments/update", content);
                        if (responseReadAllNotifications.IsSuccessStatusCode)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                }

            }

            return RedirectToAction("notifications", "Comment");
        }

        private async Task<ApiDataResponse<CommentDetail>> GetCommentResponseMessage(HttpResponseMessage responseMessage)
        {
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ApiDataResponse<CommentDetail>>(responseContent);
        }
    }
}
