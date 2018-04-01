using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordSwapper.Settings
{
    public class CustomDesignTimeService : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
                    => serviceCollection.AddSingleton<IPluralizer, CustomPluralizer>();
    }
}
