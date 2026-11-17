using System.Net;
using AppTemplate.Presentation.Client.Generated;
using AppTemplate.FrameworkSite.Common.Hosting;
using AppTemplate.FrameworkSite.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AppTemplate.FrameworkSite.UnitTests.Common.Hosting;

public sealed class SignInCompositionTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheApp_RefusesToStart_UntilTheSignInIsConfigured()
    {
        using var services = SiteServices.Build(NoContent(), settings: new Dictionary<string, string?>
        {
            [$"{SignInOptions.SectionName}:Authority"] = string.Empty,
            [$"{SignInOptions.SectionName}:ClientId"] = string.Empty,
        });

        var refused = await Should.ThrowAsync<OptionsValidationException>(ValidateOnStartAsync(services));

        refused.Message.ShouldContain("'SignIn:Authority'");
        refused.Message.ShouldContain("'SignIn:ClientId'");
    }

    [Fact]
    public async Task TheApp_Starts_OnceTheSignInIsConfigured()
    {
        using var services = SiteServices.Build(NoContent());

        await ValidateOnStartAsync(services);
    }

    [Fact]
    public void TheProvider_IsTheSignInSettings_ThroughTheAuthorizationCodeFlow()
    {
        using var services = SiteServices.Build(NoContent());
        using var scope = services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>().Value.ProviderOptions;

        provider.Authority.ShouldBe(SiteServices.Authority);
        provider.ClientId.ShouldBe(SiteServices.ClientId);
        provider.ResponseType.ShouldBe("code");
        provider.DefaultScopes.ShouldBe(["openid", "profile", "email"]);
        provider.RedirectUri.ShouldBe($"{AppNavigationManager.BaseAddress}authentication/login-callback");
    }

    [Fact]
    public async Task TheClient_CallsTheApi_WithTheProvidersAccessToken_OnceSignedIn()
    {
        var tokens = new StubAccessTokenProvider();
        var presented = new List<string?>();
        var api = new StubApi(request =>
        {
            presented.Add(request.Headers.Authorization?.ToString());

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var services = SiteServices.Build(api, tokens);
        var client = services.GetRequiredService<ApiClient>();

        await client.Api.V1.ExternalIdentities[Guid.NewGuid()].DeleteAsync(cancellationToken: Cancellation);
        tokens.AccessToken = "a1";
        await client.Api.V1.ExternalIdentities[Guid.NewGuid()].DeleteAsync(cancellationToken: Cancellation);

        presented.ShouldBe([null, "Bearer a1"]);
    }

    private static StubApi NoContent() => new(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

    /*#if (!dotnet11)*/
    private static Task ValidateOnStartAsync(IServiceProvider provider) =>
        Task.Run(provider.GetRequiredService<IStartupValidator>().Validate, TestContext.Current.CancellationToken);
    /*#else
    private static Task ValidateOnStartAsync(IServiceProvider provider) =>
        provider.GetRequiredService<IAsyncStartupValidator>().ValidateAsync(TestContext.Current.CancellationToken);
    #endif*/
}
