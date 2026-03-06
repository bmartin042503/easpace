using System;
using easpace.Desktop.Constants;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Factories;

public class PageFactory(Func<ApplicationPage, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPage page) => factory.Invoke(page);
}
