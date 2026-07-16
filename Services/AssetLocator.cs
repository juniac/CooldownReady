using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace CooldownReady.Services
{
    /// <summary>
    /// Assets 폴더 경로를 확인합니다. 패키징된 앱과 unpackaged 직접 실행을 모두 지원합니다.
    /// </summary>
    public static class AssetLocator
    {
        public static string GetAssetsPath(string relativePath)
        {
            try
            {
                var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                if (packageLocation != null)
                {
                    var packagePath = Path.Combine(packageLocation.Path, relativePath);
                    if (File.Exists(packagePath) || Directory.Exists(packagePath))
                    {
                        return packagePath;
                    }
                }
            }
            catch
            {
                // 패키징되지 않은 앱인 경우
            }

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                // 단일 파일로 빌드된 경우
                assemblyDirectory = AppContext.BaseDirectory;
            }
            return Path.Combine(assemblyDirectory, relativePath);
        }

        public static async Task<StorageFolder?> GetAssetsFolderAsync(string relativePath)
        {
            try
            {
                var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                if (packageLocation != null)
                {
                    try
                    {
                        var folderPath = Path.Combine(packageLocation.Path, relativePath);
                        return await StorageFolder.GetFolderFromPathAsync(folderPath);
                    }
                    catch
                    {
                        // 폴더를 찾을 수 없는 경우
                    }
                }
            }
            catch
            {
                // 패키징되지 않은 앱인 경우
            }

            var assetsPath = GetAssetsPath(relativePath);
            if (Directory.Exists(assetsPath))
            {
                try
                {
                    return await StorageFolder.GetFolderFromPathAsync(assetsPath);
                }
                catch
                {
                    // 폴더를 열 수 없는 경우
                }
            }

            return null;
        }
    }
}
