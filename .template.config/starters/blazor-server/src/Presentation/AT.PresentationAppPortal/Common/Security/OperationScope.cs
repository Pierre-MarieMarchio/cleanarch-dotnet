using Microsoft.AspNetCore.Components.Authorization;

namespace AppTemplate.PresentationAppPortal.Common.Security;

public sealed class OperationScope : IDisposable
{
    private readonly IServiceScopeFactory _scopes;

    private readonly AuthenticationStateProvider _authentication;

    private Task<AuthenticationState> _state;

    public OperationScope(IServiceScopeFactory scopes, AuthenticationStateProvider authentication)
    {
        ArgumentNullException.ThrowIfNull(authentication);

        _scopes = scopes;
        _authentication = authentication;
        _state = authentication.GetAuthenticationStateAsync();
        authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> operation)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);

        var state = await _state;

        await using var scope = _scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CallerPrincipal>().Principal = state.User;

        return await operation(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public void Dispose() => _authentication.AuthenticationStateChanged -= OnAuthenticationStateChanged;

    private void OnAuthenticationStateChanged(Task<AuthenticationState> state) => _state = state;
}
