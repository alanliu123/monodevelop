﻿//
// ReinstallNuGetPackageAction.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using NuGet.PackageManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class ReinstallNuGetPackageAction : IPackageAction
	{
		NuGetPackageManager packageManager;
		NuGetProjectContext context;
		CancellationToken cancellationToken;
		InstallNuGetPackageAction installAction;
		UninstallNuGetPackageAction uninstallAction;

		public ReinstallNuGetPackageAction (
			IDotNetProject project,
			IMonoDevelopSolutionManager solutionManager,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.cancellationToken = cancellationToken;
			context = new NuGetProjectContext ();

			var restartManager = new DeleteOnRestartManager ();

			packageManager = new NuGetPackageManager (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				restartManager
			);

			CreateInstallAction (solutionManager, project);
			CreateUninstallAction (solutionManager, project);
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }

		public void Execute ()
		{
			using (IDisposable fileMonitor = CreateFileMonitor ()) {
				uninstallAction.PackageId = PackageId;
				uninstallAction.Execute ();

				installAction.PackageId = PackageId;
				installAction.Version = Version;
				installAction.LicensesMustBeAccepted = false;
				installAction.Execute ();
			}
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		void CreateUninstallAction (IMonoDevelopSolutionManager solutionManager, IDotNetProject project)
		{
			uninstallAction = new UninstallNuGetPackageAction (
				solutionManager,
				project,
				cancellationToken) {
				ForceRemove = true
			};
		}

		void CreateInstallAction (IMonoDevelopSolutionManager solutionManager, IDotNetProject project)
		{
			installAction = new InstallNuGetPackageAction (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ().GetRepositories (),
				solutionManager,
				project,
				context,
				cancellationToken
			);
		}

		IDisposable CreateFileMonitor ()
		{
			return new PreventPackagesConfigFileBeingRemovedOnUpdateMonitor (
				PackageManagementServices.PackageManagementEvents,
				new FileRemover ());
		}
	}
}

