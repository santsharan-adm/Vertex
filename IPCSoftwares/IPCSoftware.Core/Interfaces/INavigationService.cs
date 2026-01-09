using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Windows.Controls;

namespace IPCSoftware.Core.Interfaces
{
    public interface INavigationService
    {
     /*   void Configure(ContentControl mainHost, ContentControl topHost);

        void NavigateMain<TView>() where TView : class, new();

        void NavigateTop<TView>() where TView : class, new();
        void ClearTop();
        void ClearMain();*/
        void NavigateTop(object view);


        void Configure(ContentControl mainContent, ContentControl ribbonHost);
        void NavigateMain<TView>() where TView : UserControl;
        void NavigateRibbon<TView>() where TView : UserControl;
        void ClearTop();

        // NEW: For log configuration navigation
        void NavigateToLogConfiguration(LogConfigurationModel logToEdit, Func<Task> onSaveCallback);

        // Device Configuration
        void NavigateToDeviceList();
        void NavigateToDeviceConfiguration(DeviceModel deviceToEdit, Func<Task> onSaveCallback);
        void NavigateToDeviceDetail(DeviceModel device);
        void NavigateToCameraDetail(DeviceModel device);
        void NavigateToInterfaceConfiguration(DeviceModel parentDevice, DeviceInterfaceModel interfaceToEdit, Func<Task> onSaveCallback);
        void NavigateToCameraInterfaceConfiguration(DeviceModel parentDevice, CameraInterfaceModel cameraInterfaceToEdit, Func<Task> onSaveCallback);


        // Alarm Configuration - NEW
        void NavigateToAlarmList();
        void NavigateToAlarmConfiguration(AlarmConfigurationModel alarmToEdit, Func<Task> onSaveCallback);

        // User Management - NEW
        void NavigateToUserList();
        void NavigateToUserConfiguration(UserConfigurationModel userToEdit, Func<Task> onSaveCallback);

        // System Settings
        void NavigateToSystemSettings();
        // PLC Tag Configuration - NEW
        void NavigateToPLCTagList();

        void NavigateToLogs(LogType logType);
        void NavigateToPLCTagConfiguration(PLCTagConfigurationModel tagToEdit, Func<Task> onSaveCallback);
        bool CanNavigateFromCurrent();

    }

    public interface INavigationalAware
    {
        // Returns True if navigation is allowed, False to cancel
        bool OnNavigatingFrom();
    }
}
