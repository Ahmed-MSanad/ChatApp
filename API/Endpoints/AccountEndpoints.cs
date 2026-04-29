using API.Common;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints;

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndPoint(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/account").WithTags("account");

        group.MapPost("/register", async (HttpContext context, UserManager<AppUser> userManager, 
                                            [FromForm] string fullName, [FromForm] string email,
                                            [FromForm] string password, [FromForm] string userName,
                                            [FromForm] IFormFile? profileImage) =>
        {
            var userFromDb = await userManager.FindByEmailAsync(email);

            if(userFromDb is not null)
                return Results.BadRequest(Response<string>.Failure("User is already exist."));

            if(profileImage is null)
                return Results.BadRequest(Response<string>.Failure("Profile Image is required."));
            
            var image = await FileUpload.Upload(profileImage); // Store the Uploaded File on the Server and just store the File Name in the Database
            image = $"{context.Request.Scheme}://{context.Request.Host}/uploads/{image}";

            var user = new AppUser
            {
                Email = email,
                FullName = fullName,
                UserName = userName,
                ProfileImage = image
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return Results.BadRequest(Response<string>.Failure(result.Errors.Select(x => x.Description).FirstOrDefault()!));
            return Results.Ok(Response<string>.Success("User Created Successfully."));
        }).DisableAntiforgery();

        return group;
    }
}