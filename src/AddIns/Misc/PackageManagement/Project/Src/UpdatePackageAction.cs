﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdatePackageAction : ProcessPackageAction
	{
		public UpdatePackageAction(
			IPackageManagementService packageManagementService,
			IPackageManagementEvents packageManagementEvents)
			: base(packageManagementService, packageManagementEvents)

		{
			UpdateDependencies = true;
		}
		
		public IEnumerable<PackageOperation> Operations { get; set; }
		public bool UpdateDependencies { get; set; }
		
		protected override void BeforeExecute()
		{
			base.BeforeExecute();
			GetPackageOperationsIfMissing();
		}
				
		void GetPackageOperationsIfMissing()
		{
			if (Operations == null) {
				Operations = PackageManager.GetInstallPackageOperations(Package, !UpdateDependencies);
			}
		}
		
		protected override void ExecuteCore()
		{
			PackageManager.UpdatePackage(Package, Operations, UpdateDependencies);
			OnParentPackageInstalled();
		}
	}
}