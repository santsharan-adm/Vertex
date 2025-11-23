using System.Windows.Controls;

namespace IPCSoftware.Core.Interfaces
{
    public interface INavigationService
    {
        void Configure(ContentControl mainHost, ContentControl topHost);

        void NavigateMain<TView>() where TView : class, new();
        //void NavigateTop<TView>() where TView : class, new();
        void NavigateTop(object view);

        void ClearTop();
        void ClearMain();
    }
}
