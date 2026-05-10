using System.Net.Http.Headers;
using System.Security.Claims;
using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public class AccountController(
    InternalHttpClient http) : ControllerBase
{
    // =========================
    // GET SINGLE ACCOUNT
    // =========================
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<Response<AccountResponse>>> GetAccount(Guid userId)
        => await Forward<AccountResponse>($"/api/account/{userId}");

    // =========================
    // GET USER ACCOUNTS
    // =========================
    [HttpGet("all/{userId:guid}")]
    public async Task<ActionResult<Response<IEnumerable<AccountResponse>>>> GetAccounts(Guid userId)
        => await Forward<IEnumerable<AccountResponse>>($"/api/account/all/{userId}");

    // =========================
    // BUSINESS
    // =========================
    [HttpGet("all/business")]
    public async Task<ActionResult<Response<IEnumerable<BusinessAccountResponse>>>> GetAllBusiness()
        => await Forward<IEnumerable<BusinessAccountResponse>>("/api/account/all/business");

    // =========================
    // CREATE
    // =========================
    [HttpPost("customer")]
    public Task<ActionResult<Response<AccountResponse>>> CreateCustomer(
        [FromForm] CreateCustomerAccountRequest request)
        => ForwardMultipart<AccountResponse>("customer", request);

    [HttpPost("business")]
    public Task<ActionResult<Response<AccountResponse>>> CreateBusiness(
        [FromForm] CreateBusinessAccountRequest request)
        => ForwardMultipart<AccountResponse>("business", request);

    [HttpPost("courier")]
    public Task<ActionResult<Response<AccountResponse>>> CreateCourier(
        [FromForm] CreateCourierAccountRequest request)
        => ForwardMultipart<AccountResponse>("courier", request);

    // =========================
    // UPDATE
    // =========================
    [HttpPut("customer")]
    public Task<ActionResult<Response<AccountResponse>>> UpdateCustomer(
        [FromForm] UpdateCustomerAccountRequest request)
        => ForwardMultipart<AccountResponse>("customer", request);

    [HttpPut("business")]
    public Task<ActionResult<Response<AccountResponse>>> UpdateBusiness(
        [FromForm] UpdateBusinessAccountRequest request)
        => ForwardMultipart<AccountResponse>("business", request);

    [HttpPut("courier")]
    public Task<ActionResult<Response<AccountResponse>>> UpdateCourier(
        [FromForm] UpdateCourierAccountRequest request)
        => ForwardMultipart<AccountResponse>("courier", request);

    // =========================
    // ONBOARDING
    // =========================
    [HttpGet("onboarding/{businessId:guid}")]
    public async Task<ActionResult<Response<string>>> GetOnboardingLink(Guid businessId)
        => await Forward<string>($"/api/account/onboarding/{businessId}");

    // =========================
    // INTERNAL CALL
    // =========================

    private async Task<ActionResult<Response<T>>> Forward<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        var response = await http.SendAsync(request, userId);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ServiceErrorResponse>();

            return Ok(new Response<T>(
                false,
                default,
                error?.Message ?? "Account error"
            ));
        }

        var data = await response.Content.ReadFromJsonAsync<T>();
        return Ok(new Response<T>(true, data!));
    }

    private async Task<ActionResult<Response<T>>> ForwardMultipart<T>(
        string accountType,
        object requestModel)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        var form = new MultipartFormDataContent();

        foreach (var prop in requestModel.GetType().GetProperties())
        {
            var value = prop.GetValue(requestModel);
            if (value is null) continue;

            if (value is IFormFile file)
            {
                form.Add(
                    new StreamContent(file.OpenReadStream())
                    {
                        Headers =
                        {
                            ContentType = MediaTypeHeaderValue.Parse(file.ContentType)
                        }
                    },
                    prop.Name,
                    file.FileName
                );
            }
            else
            {
                form.Add(new StringContent(value.ToString()!), prop.Name);
            }
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/account/{accountType}")
        {
            Content = form
        };

        var response = await http.SendAsync(request, userId);

        if (!response.IsSuccessStatusCode)
            return await MapError<T>(response);

        var data = await response.Content.ReadFromJsonAsync<T>();
        return Ok(new Response<T>(true, data!));
    }

    private async Task<ActionResult<Response<T>>> MapError<T>(
        HttpResponseMessage response)
    {
        var error = await response.Content
            .ReadFromJsonAsync<ServiceErrorResponse>();

        return Ok(new Response<T>(
            false,
            default,
            error?.Message ?? "Unknown error"
        ));
    }
}