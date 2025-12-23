using IPCSoftware.App;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
namespace IPCSoftware.App.Test
{
 public class LoginViewModelTests
 {
 private readonly Mock<IAuthService> _authMock = new();
 private readonly Mock<INavigationService> _navMock = new();
 private readonly Mock<IDialogService> _dialogMock = new();
 private readonly Mock<IAppLogger> _loggerMock = new();
        MainWindowViewModel? mainWindowViewModel;

        public LoginViewModelTests()
           {
                 // Ensure App.ServiceProvider is set for tests that call AppInitializationService
            var services = new ServiceCollection();
            services.AddSingleton<INavigationService>(_ => _navMock.Object);
            services.AddSingleton<RibbonViewModel>();
            services.AddSingleton<OEEDashboard>();
            services.AddSingleton<DashboardView>();
             // App.ServiceProvider = services.BuildServiceProvider();

             // Replace direct assignment to App.ServiceProvider with reflection to set the private setter
             var serviceProviderProperty = typeof(App).GetProperty("ServiceProvider", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
             serviceProviderProperty.SetValue(null, services.BuildServiceProvider());
            //var logerrorProperty = typeof(App).GetProperty("Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            // logerrorProperty.SetValue(null, _loggerMock.Object);


            var spProp = typeof(App).GetProperty("ServiceProvider",BindingFlags.Static | BindingFlags.Public);
            spProp.SetValue(null, services.BuildServiceProvider()); 

        }


         [Fact]
          public void Constructor_Sets_IsUsernameFocused_And_LoginCommand_NotNull()
            {
             var vm = new LoginViewModel(_authMock.Object, _navMock.Object, _dialogMock.Object, null, _loggerMock.Object);
               Assert.True(vm.IsUsernameFocused);
               Assert.NotNull(vm.LoginCommand);
             }

          [Fact]
          public async Task ExecuteLoginAsync_ShowsMessage_When_UsernameOrPassword_Empty()
            {
              var vm = new LoginViewModel(_authMock.Object, _navMock.Object, _dialogMock.Object, null, _loggerMock.Object);
              vm.Username = "";
              vm.Password = "";

               // Use reflection to invoke private method
            var method = typeof(LoginViewModel).GetMethod("ExecuteLoginAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method.Invoke(vm, null);
            await task;

             _dialogMock.Verify(x => x.ShowMessage("Please enter username and password."), Times.Once);
             }
          
          
          

           [Fact]
          public async Task ExecuteLoginAsync_ShowsInvalid_On_FailedAuth()
             {
               _authMock.Setup(a => a.LoginAsync("user","pass")).ReturnsAsync((false, (string)null));
               var vm = new LoginViewModel(_authMock.Object, _navMock.Object, _dialogMock.Object, null, _loggerMock.Object);
               vm.Username = "user";
               vm.Password = "pass";

               var method = typeof(LoginViewModel).GetMethod("ExecuteLoginAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
               var task = (Task)method.Invoke(vm, null);
               await task;

               _loggerMock.Verify(l => l.LogError(
    It.Is<string>(s => s.Contains("Login failed")),
    It.IsAny<LogType>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<int>()), Times.Once);
               _dialogMock.Verify(d => d.ShowMessage("Invalid username or password."), Times.Once);
              }

          [Fact]
           public async Task ExecuteLoginAsync_Handles_Exception_From_Auth_And_ShowsMessage()
             {
                _authMock.Setup(a => a.LoginAsync("user","pass")).ThrowsAsync(new System.Exception("boom"));
                var vm = new LoginViewModel(_authMock.Object, _navMock.Object, _dialogMock.Object, null, _loggerMock.Object);
                vm.Username = "user";
                vm.Password = "pass";

                var method = typeof(LoginViewModel).GetMethod("ExecuteLoginAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var task = (Task)method.Invoke(vm, null);
                await task;

                 _loggerMock.Verify(l => l.LogError(
    It.Is<string>(s => s.Contains("Login error")),
    It.IsAny<LogType>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<int>()), Times.Once);
                 _dialogMock.Verify(d => d.ShowMessage(It.Is<string>(s => s.Contains("boom"))), Times.Once);
             }

           [Fact]
           public async Task ExecuteLoginAsync_On_Success_Sets_UserSession()
              {
                _authMock.Setup(a => a.LoginAsync("admin","pass")).ReturnsAsync((true, "Admin"));
                var nav = new Mock<INavigationService>();
                var vm = new LoginViewModelForTest(_authMock.Object, nav.Object, _dialogMock.Object, _loggerMock.Object, null );
                vm.Username = "admin";
                vm.Password = "pass";

                var method = typeof(LoginViewModel).GetMethod("ExecuteLoginAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var task = (Task)method.Invoke(vm, null);
                await task;

                Assert.Equal("admin", UserSession.Username);
                Assert.Equal("Admin", UserSession.Role);
                  }



        // Test-specific subclass to override view creation to avoid WPF types
        private class LoginViewModelForTest : LoginViewModel
              {
                public LoginViewModelForTest(IAuthService authService, INavigationService navigation, IDialogService dialog, IAppLogger logger, MainWindowViewModel? mw)
    : base(authService, navigation, dialog, mw, logger)
                {
                  }

              protected object CreateRibbonView(object ribbonVM)
              {
                // Return a plain object instead of a WPF control
                return new { Name = "Ribbon" };
               }
                }
  }
}
