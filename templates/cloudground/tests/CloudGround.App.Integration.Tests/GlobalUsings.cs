global using System.Globalization;
global using System.Net;
global using System.Net.Http.Json;
global using System.Reflection;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.RegularExpressions;
global using System.Web;
global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
global using Aspire.Hosting.Docker;
global using Aspire.Hosting.Docker.Resources;
global using Aspire.Hosting.Docker.Resources.ComposeNodes;
global using Aspire.Hosting.EntityFrameworkCore;
global using Aspire.Hosting.Testing;
global using CloudGround.App.AppHost;
global using Microsoft.Extensions.DependencyInjection;
global using Npgsql;
global using StackExchange.Redis;
global using Xunit;
#if (sample)
global using static CloudGround.App.Integration.Tests.ModuleAssertions;
#endif
