using System.Text.RegularExpressions;
using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Blog.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Blog.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    [HttpPost("v1/accounts/")]
    public async Task<IActionResult> Post(
        [FromBody] RegisterViewModel model,
        [FromServices] BlogDataContext context,
        [FromServices] EmailService emailService)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Slug = model.Email.Replace("@", "-").Replace(".", "-")
        };

        var password = PasswordGenerator.Generate(25);
        user.PasswordHash = PasswordHasher.Hash(password);

        try
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            emailService.Send(user.Name, user.Email, "Bem vindo ao blog!", $"Sua senha é {password}");
            return Ok(new ResultViewModel<dynamic>(new
            {
                user = user.Email, password
            }));
        }
        catch (DbUpdateException)
        {
            return StatusCode(400, new ResultViewModel<string>("05X99 - Este E-mail já está cadastrado"));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor"));
        }
    }

    [HttpPost("v1/accounts/login")] 
    public async Task<IActionResult> Login(
        [FromBody] LoginViewModel model,
        [FromServices] BlogDataContext context,
        [FromServices] TokenService tokenService)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var user = await context
            .Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Email == model.Email);

        if (user == null)
            return StatusCode(401, new ResultViewModel<string>("Usuário ou senha inválidos"));

        if (!PasswordHasher.Verify(user.PasswordHash, model.Password))
            return StatusCode(401, new ResultViewModel<string>("Usuário ou senha inválidos"));

        try
        {
            var token = tokenService.GenerateToken(user);
            return Ok(new ResultViewModel<string>(token, null));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no servidor"));
        }
    }
    
    [Authorize]
    [HttpPost("v1/accounts/upload-image")]
    public async Task<IActionResult> UploadImage(
        [FromBody] UploadImageViewModel model,
        [FromServices] BlogDataContext context)
    {
        if (string.IsNullOrWhiteSpace(model.Base64Image))
            return BadRequest(new ResultViewModel<string>("Imagem não enviada."));

        var base64Data = model.Base64Image.Contains(",")
            ? model.Base64Image.Substring(model.Base64Image.IndexOf(",") + 1)
            : model.Base64Image;

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Erro ao converter Base64: {ex.Message}");
            return BadRequest(new ResultViewModel<string>("Base64 inválida!"));
        }

        var fileName = $"{Guid.NewGuid()}.jpg";
        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

        try
        {
            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            var fullPath = Path.Combine(imagePath, fileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, imageBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar imagem: {ex.Message}");
            return StatusCode(500, new ResultViewModel<string>("Erro ao salvar a imagem no servidor."));
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
        if (user == null)
            return NotFound(new ResultViewModel<string>("Usuário não encontrado."));

        // Monta URL absoluta dinamicamente
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        user.Image = $"{baseUrl}/images/{fileName}";

        try
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar usuário: {ex.Message}");
            return StatusCode(500, new ResultViewModel<string>("Erro ao atualizar o usuário."));
        }

        return Ok(new ResultViewModel<string>("Imagem alterada com sucesso!"));

    }
}
