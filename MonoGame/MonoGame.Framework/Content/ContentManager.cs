// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;

namespace Microsoft.Xna.Framework.Content
{
    //Replaced with a stub implementation. XenoKit does not need this
	public partial class ContentManager : IDisposable
	{
		private IServiceProvider serviceProvider;
		
		public ContentManager(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			this.serviceProvider = serviceProvider;
		}

		public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			if (rootDirectory == null)
			{
				throw new ArgumentNullException("rootDirectory");
			}
			this.RootDirectory = rootDirectory;
			this.serviceProvider = serviceProvider;
		}

		public void Dispose()
		{
			
		}
		
		protected virtual void Dispose(bool disposing)
		{
			
		}

		public virtual T LoadLocalized<T> (string assetName)
		{
				throw new NotImplementedException();
		}

		public virtual T Load<T>(string assetName)
		{
				throw new NotImplementedException();
		}
		
		protected virtual Stream OpenStream(string assetName)
		{
				throw new NotImplementedException();
		}

		protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
		{
				throw new NotImplementedException();
		}
		
		internal void RecordDisposable(IDisposable disposable)
		{
			
		}
		
		public virtual void Unload()
		{
			
		}

		public string RootDirectory { get; set;}

        internal string RootDirectoryFullPath { get { return RootDirectory; } }
		
		
		public IServiceProvider ServiceProvider
		{
			get
			{
				return this.serviceProvider;
			}
		}
    }
}
