using API.Common;
using API.Dtos;
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

        group.MapPost("/register", async (HttpContext context, UserManager<AppUser> userManager, [FromForm] RegisterDto registerDto) =>
        {
            var userFromDb = await userManager.FindByEmailAsync(registerDto.Email);

            if(userFromDb is not null)
                return Results.BadRequest(Response<string>.Failure("User is already exist."));

            if(registerDto.ProfileImage is null)
                return Results.BadRequest(Response<string>.Failure("Profile Image is required."));
            
            var image = await FileUpload.Upload(registerDto.ProfileImage); // Store the Uploaded File on the Server and just store the File Name in the Database
            image = $"{context.Request.Scheme}://{context.Request.Host}/uploads/{image}";

            var user = new AppUser
            {
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                UserName = registerDto.UserName,
                ProfileImage = image
            };
            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return Results.BadRequest(Response<string>.Failure(result.Errors.Select(x => x.Description).FirstOrDefault()!));
            return Results.Ok(Response<string>.Success("User Created Successfully."));
        }).DisableAntiforgery();


        group.MapPost("/login", async(UserManager<AppUser> userManager, TokenService token, LoginDto loginDto) =>
        {
            if(loginDto is null)
                return Results.BadRequest(Response<string>.Failure("Invalid Login Request!"));

            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if(user is null)
                return Results.BadRequest(Response<string>.Failure("User not found!"));

            var checkPassword = await userManager.CheckPasswordAsync(user, loginDto.Password);
            if(!checkPassword)
                return Results.BadRequest(Response<string>.Failure("Invalid Password!"));

            var resultToken = token.Generate(user.Id, user.UserName!);
            if(!string.IsNullOrEmpty(resultToken))
                return Results.Ok(Response<string>.Success(resultToken!, "User Logged in successfully!"));
            return Results.BadRequest(Response<string>.Failure("Error while generating the token!"));
        });

        return group;
    }
}