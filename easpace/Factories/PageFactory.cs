using System;
using easpace.Constants;
using easpace.ViewModels;

namespace easpace.Factories;

public class PageFactory(Func<ApplicationPage, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPage page) => factory.Invoke(page);
}