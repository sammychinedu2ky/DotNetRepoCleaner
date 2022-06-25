using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Octokit;
using Octokit.Internal;
using AspNet.Security.OAuth.GitHub;

namespace DotNetRepoCleaner.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ILogger<DashboardModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public List<MyRepository> Repositories { get; set; } = new();
        public async Task<IActionResult> OnGet()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return Challenge();
            }
            var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
            var creds = new InMemoryCredentialStore(new Credentials(accessToken));
            var github = new GitHubClient(new ProductHeaderValue("RepoCleaner"), creds);
            var repos = await github.Repository.GetAllForUser(User.Identity!.Name);
            Repositories = repos.Select(i => new MyRepository
            {
                Id = i.Id,
                Name = i.Name,
                Url = i.HtmlUrl,
            }).ToList();
         
            return Page();
        }
        public async Task<IActionResult> OnPost()
        {
            var selected = Repositories.Where(i => i.Selected == true);
            Console.WriteLine(selected);
            var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
            var creds = new InMemoryCredentialStore(new Credentials(accessToken));
            var github = new GitHubClient(new ProductHeaderValue("RepoCleaner"), creds);
            foreach(var repo in selected)
            {
                try
                {
                    await github.Repository.Delete(repo.Id);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return Redirect("/"); 
        }

        public async Task<IActionResult> OnPostLogout()
        {
           await HttpContext.SignOutAsync();

          return  Redirect("https://github.com/logout");
        }
        public class MyRepository 
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            public string? Url { get; set; }
            public bool Selected { get; set; }
        }
    }

}