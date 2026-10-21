# Docker Desktop may be running while this terminal has an older PATH.
if (-not (Get-Command docker -ErrorAction SilentlyContinue) -and $IsWindows) {
    $desktopRoots = @(
        (Join-Path $env:ProgramFiles 'Docker/Docker'),
        (Join-Path $env:LOCALAPPDATA 'Programs/DockerDesktop'),
        (Join-Path $env:LOCALAPPDATA 'Docker')
    )
    foreach ($desktopRoot in $desktopRoots) {
        $cliDirectory = Join-Path $desktopRoot 'resources/bin'
        if (Test-Path -LiteralPath (Join-Path $cliDirectory 'docker.exe') -PathType Leaf) {
            $env:PATH = $cliDirectory + [IO.Path]::PathSeparator + $env:PATH
            break
        }
    }
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Cannot find the Docker CLI in this terminal. Ensure docker version works here, or reopen the terminal after installing Docker. A running engine alone does not put the CLI on PATH.'
}
