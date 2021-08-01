// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Data.Converters")]
#if SIGNED_BUILD
[assembly: InternalsVisibleTo("Avalonia.Base.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
[assembly: InternalsVisibleTo("Avalonia.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")] 
[assembly: InternalsVisibleTo("Avalonia.Controls.DataGrid, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
[assembly: InternalsVisibleTo("Avalonia.Markup.Xaml.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
[assembly: InternalsVisibleTo("Avalonia.Visuals, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
#else
[assembly: InternalsVisibleTo("Avalonia.Base.UnitTests")]
[assembly: InternalsVisibleTo("Avalonia.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Avalonia.Controls.DataGrid")]
[assembly: InternalsVisibleTo("Avalonia.Markup.Xaml.UnitTests")]
[assembly: InternalsVisibleTo("Avalonia.Visuals")]
#endif
