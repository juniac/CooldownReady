using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace CooldownReady.Services
{
    /// <summary>
    /// Assets\sounds 폴더의 알림음 목록 조회와 재생을 담당합니다.
    /// </summary>
    public class AlertSoundService : IDisposable
    {
        private const string SoundsFolder = "Assets\\sounds";

        private readonly MediaPlayer _mediaPlayer = new()
        {
            AudioCategory = MediaPlayerAudioCategory.Alerts
        };

        /// <summary>
        /// 사용 가능한 mp3 파일명 목록을 이름순으로 반환합니다.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetSoundFileNamesAsync()
        {
            try
            {
                var soundsFolder = await AssetLocator.GetAssetsFolderAsync(SoundsFolder);
                if (soundsFolder == null)
                {
                    System.Diagnostics.Debug.WriteLine("sounds 폴더를 찾을 수 없습니다.");
                    return Array.Empty<string>();
                }

                var files = await soundsFolder.GetFilesAsync();
                return files.Where(f => f.FileType.ToLower() == ".mp3")
                            .Select(f => f.Name)
                            .OrderBy(name => name)
                            .ToList();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("사운드 파일 로드 오류", ex);
                return Array.Empty<string>();
            }
        }

        public async Task PlayAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            try
            {
                string soundPath = AssetLocator.GetAssetsPath(Path.Combine(SoundsFolder, fileName));
                MediaSource source;

                if (File.Exists(soundPath))
                {
                    // 직접 실행 파일인 경우 파일 경로 사용
                    var soundFile = await StorageFile.GetFileFromPathAsync(soundPath);
                    source = MediaSource.CreateFromStorageFile(soundFile);
                }
                else
                {
                    // 패키징된 앱인 경우 ms-appx URI 사용
                    source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/sounds/{fileName}"));
                }

                _mediaPlayer.Source = source;
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("사운드 재생 오류", ex);
            }
        }

        public void Dispose()
        {
            _mediaPlayer.Dispose();
        }
    }
}
