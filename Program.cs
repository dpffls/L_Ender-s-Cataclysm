using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ModAutoInstaller;

internal class ModEntry
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";   // 파일이 안 깨졌는지 확인하는 "지문" (비어있으면 검사 생략)
}

internal class Program
{
    // ─────────────────────────────────────────────────────────
    // ★★★ 아래 4줄만 본인 정보에 맞게 수정하세요 ★★★
    //
    // GITHUB_USER : 깃허브 아이디
    // GITHUB_REPO : 새로 만든 저장소 이름
    // RELEASE_TAG : 릴리즈에 붙인 이름 (모드 파일을 올린 릴리즈, 예: v1)
    // FORGE_VERSION : 서버의 Forge 버전 번호 (예: 47.3.0)
    //                 ※ 모드 중 하나가 47.0.34 이상을 요구하므로 그보다 낮으면 안 됩니다.
    // ─────────────────────────────────────────────────────────
    private const string GITHUB_USER = "dpffls";
    private const string GITHUB_REPO = "L_Ender-s-Cataclysm";
    private const string RELEASE_TAG = "v1";
    private const string FORGE_VERSION = "47.4.26";

    // 마인크래프트 버전 (모드들이 전부 1.20.1용)
    private const string MC_VERSION = "1.20.1";

    // 아래 두 주소는 위 값으로 자동 완성됩니다 (건드리지 않아도 됨)
    private const string MODS_JSON_URL =
        $"https://raw.githubusercontent.com/{GITHUB_USER}/{GITHUB_REPO}/main/mods.json";
    private const string FORGE_INSTALLER_URL =
        $"https://github.com/{GITHUB_USER}/{GITHUB_REPO}/releases/download/{RELEASE_TAG}/forge-installer.jar";

    private static readonly HttpClient http = new();

    private static string MinecraftDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");

    private static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================");
        Console.WriteLine(" 규민 서버 자동 설치기 (Forge 1.20.1)");
        Console.WriteLine("================================\n");

        try
        {
            CheckMinecraftFolder();
            await EnsureForgeInstalledAsync();
            await DownloadModsAsync();

            Console.WriteLine("\n모든 설치가 끝났습니다! 이제 마인크래프트 런처에서");
            Console.WriteLine($"'forge {MC_VERSION}' 프로필을 골라 접속하시면 됩니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[오류] 설치 중 문제가 발생했습니다: {ex.Message}");
        }

        Console.WriteLine("\n아무 키나 누르면 종료합니다...");
        Console.ReadKey();
    }

    // ── 0. 사전 확인: 마인크래프트 런처를 한 번이라도 켠 적 있는지 ──
    private static void CheckMinecraftFolder()
    {
        if (!File.Exists(Path.Combine(MinecraftDir, "launcher_profiles.json")))
        {
            throw new Exception(
                "마인크래프트 런처를 한 번도 실행하지 않은 것 같습니다.\n" +
                "  → 런처를 열어서 1.20.1 버전을 한 번 실행(메인 화면까지)한 뒤, 이 프로그램을 다시 켜주세요.");
        }
    }

    // ── 1. Forge 설치 (이미 설치돼 있으면 건너뜀) ──────────────
    private static async Task EnsureForgeInstalledAsync()
    {
        string versionsDir = Path.Combine(MinecraftDir, "versions");
        string forgeFolder = Path.Combine(versionsDir, $"{MC_VERSION}-forge-{FORGE_VERSION}");

        if (Directory.Exists(forgeFolder))
        {
            Console.WriteLine($"[Forge] {FORGE_VERSION} 이(가) 이미 설치되어 있습니다. 건너뜁니다.\n");
            return;
        }

        Console.WriteLine("[Forge] 설치 파일을 내려받는 중...");
        string tempJar = Path.Combine(Path.GetTempPath(), "forge-installer.jar");
        await DownloadFileAsync(FORGE_INSTALLER_URL, tempJar);

        Console.WriteLine("[Forge] 설치 프로그램을 실행합니다. (잠시 걸릴 수 있어요)");

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            // 설치 위치(.minecraft)를 직접 알려줘야 합니다. 안 알려주면 현재 폴더(.)에 설치하려다 실패해요.
            Arguments = $"-jar \"{tempJar}\" --installClient \"{MinecraftDir}\"",
            WorkingDirectory = MinecraftDir,
            UseShellExecute = false,
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
                throw new Exception("자바를 실행하지 못했습니다.");

            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
                throw new Exception($"Forge 설치가 실패했습니다. (종료 코드 {proc.ExitCode})");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new Exception(
                "자바(Java)를 찾을 수 없습니다.\n" +
                "  → 마인크래프트 1.20.1은 Java 17이 필요합니다. 설치한 뒤 다시 실행해주세요.");
        }

        Console.WriteLine("[Forge] 설치 완료.\n");
    }

    // ── 2. mods.json 을 읽어서 모드들 다운로드 ─────────────────
    private static async Task DownloadModsAsync()
    {
        Console.WriteLine("[모드 목록] 서버에서 목록을 가져오는 중...");
        string json = await http.GetStringAsync(MODS_JSON_URL);

        var mods = JsonSerializer.Deserialize<List<ModEntry>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (mods == null || mods.Count == 0)
        {
            Console.WriteLine("[모드 목록] 받아올 모드가 없습니다.");
            return;
        }

        string modsDir = Path.Combine(MinecraftDir, "mods");
        Directory.CreateDirectory(modsDir);

        Console.WriteLine($"[모드 다운로드] 총 {mods.Count}개 모드를 확인합니다.\n");

        foreach (var mod in mods)
        {
            string savePath = Path.Combine(modsDir, mod.Name);

            // 이미 있고, 지문까지 맞으면 건너뜀
            if (File.Exists(savePath) && await IsFileOkAsync(savePath, mod.Sha256))
            {
                Console.WriteLine($"  - {mod.Name} : 이미 있음, 건너뜀");
                continue;
            }

            Console.WriteLine($"  - {mod.Name} : 다운로드 중...");
            await DownloadFileAsync(mod.Url, savePath);

            if (!await IsFileOkAsync(savePath, mod.Sha256))
            {
                File.Delete(savePath);
                throw new Exception($"{mod.Name} 파일이 손상되었거나 바뀐 파일입니다. 설치를 중단합니다.");
            }
        }
    }

    // ── 파일 지문(SHA-256) 확인. 지문이 비어있으면 검사 생략 ─────
    private static async Task<bool> IsFileOkAsync(string path, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256)) return true;

        await using var stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream);
        string actual = Convert.ToHexString(hash);
        return actual.Equals(expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // ── 공통: 진행률 표시하며 파일 다운로드 ───────────────────
    private static async Task DownloadFileAsync(string url, string savePath)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            if (totalBytes.HasValue)
            {
                double percent = (double)totalRead / totalBytes.Value * 100;
                Console.Write($"\r    {percent:0}%   ");
            }
        }
        Console.WriteLine();
    }
}
