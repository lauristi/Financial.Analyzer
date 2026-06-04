# Gerar-Contexto-Arquitetural.ps1
# 1. MAPEAMENTO DE DIRETORIOS ADAPTATIVO
$diretorioDoScript = (Get-Location).Path
$diretorioRaizProjeto = (Get-Item $diretorioDoScript).Parent.FullName

# Os arquivos de saida serao gerados exatamente dentro da pasta do script
$arquivosArquitetura = Join-Path -Path $diretorioDoScript -ChildPath "architecture_summary.txt"
$arquivoCodigo = Join-Path -Path $diretorioDoScript -ChildPath "project_source_code.txt"

# 2. LIMPEZA AUTOMATICA (SOBREESCRITA)
@($arquivosArquitetura, $arquivoCodigo) | ForEach-Object {
    if (Test-Path -Path $_) { Remove-Item -Path $_ -Force | Out-Null }
}

# 3. CONFIGURACOES DE FILTROS
$pastasIgnoradas = @("bin", "obj", ".git", ".vs", "Context_I.A.", "Context_IA", "Migrations", "Properties")
$extensoesPermitidas = @(".cs", ".csproj", ".sln", ".json", ".config", ".md")

# Coleta de arquivos validos a partir da raiz do projeto
$arquivos = Get-ChildItem -Path $diretorioRaizProjeto -Recurse -File | Where-Object {
    $item = $_
    $deveIgnorar = $false
    foreach ($pasta in $pastasIgnoradas) {
        if ($item.FullName -like "*\$pasta\*" -or $item.Name -like "*AssemblyInfo*" -or $item.Name -like "*.Designer.cs" -or $item.Name -like "*IdentityModels*") {
            $deveIgnorar = $true
            break
        }
    }
    (-not $deveIgnorar) -and ($extensoesPermitidas -contains $item.Extension.ToLower())
}

# Estruturas de dados para o Indice Global e Grafos
$indiceGlobal = @{
    "CONTROLLERS" = [System.Collections.Generic.List[string]]::new()
    "SERVICES"    = [System.Collections.Generic.List[string]]::new()
    "DTOs/MODELS" = [System.Collections.Generic.List[string]]::new()
    "INTERFACES"  = [System.Collections.Generic.List[string]]::new()
    "OUTROS"      = [System.Collections.Generic.List[string]]::new()
}
$dependencyGraph = @{}
$codigoConsolidado = [System.Text.StringBuilder]::new()
$arquiteturaConsolidada = [System.Text.StringBuilder]::new()

$arquiteturaConsolidada.AppendLine("==================================================")
$arquiteturaConsolidada.AppendLine("ANALISE ARQUITETURAL DO PROJETO C#")
$arquiteturaConsolidada.AppendLine("==================================================`n")

foreach ($arquivo in $arquivos) {
    if ($arquivo.Extension -ne ".cs") { continue }
    
    $caminhoRelativo = $arquivo.FullName.Replace($diretorioRaizProjeto, "").TrimStart("\")
    $conteudo = Get-Content -Path $arquivo.FullName -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrEmpty($conteudo)) { continue }

    # --- DETECCAO DE TIPO & ARQUITETURA ---
    $nomeSemExtensao = $arquivo.BaseName
    $tipo = "Outro"
    
    if ($nomeSemExtensao -match "Controller$") { 
        $tipo = "Controller"
        $indiceGlobal["CONTROLLERS"].Add($nomeSemExtensao) 
    }
    elseif ($nomeSemExtensao -match "Service$" -or $nomeSemExtensao -match "Handler$") { 
        $tipo = "Service"
        $indiceGlobal["SERVICES"].Add($nomeSemExtensao) 
    }
    elseif ($nomeSemExtensao -match "Dto$" -or $nomeSemExtensao -match "Request$" -or $nomeSemExtensao -match "Response$" -or $conteudo -match "public record ") { 
        $tipo = "DTO/Model"
        $indiceGlobal["DTOs/MODELS"].Add($nomeSemExtensao) 
    }
    elseif ($nomeSemExtensao -like "I*" -and $nomeSemExtensao.Length -gt 1 -and [char]::IsUpper($nomeSemExtensao[1])) { 
        $tipo = "Interface"
        $indiceGlobal["INTERFACES"].Add($nomeSemExtensao) 
    }
    else { 
        $indiceGlobal["OUTROS"].Add($nomeSemExtensao) 
    }

    # --- EXTRACAO DE NAMESPACE ---
    $namespace = "Nao detectado"
    if ($conteudo -match "namespace\s+([^\s;{]+)") { $namespace = $Matches[1] }

    # --- DEPENDENCIAS DO CONSTRUTOR ---
    $dependencias = @()
    $patternConstrutor = "public\s+" + $nomeSemExtensao + "\s*\(([^)]*)\)"
    if ($conteudo -match $patternConstrutor) {
        $paramBloco = $Matches[1]
        if (-not [string]::IsNullOrWhiteSpace($paramBloco)) {
            $params = $paramBloco.Split(",")
            foreach ($param in $params) {
                $partesParam = $param.Trim().Split(" ")
                if ($partesParam.Length -ge 1) {
                    $depTipo = $partesParam[0].Trim()
                    if ($depTipo -notmatch "^(string|int|bool|Guid|DateTime|long|decimal|float)$") {
                        $dependencias += $depTipo
                    }
                }
            }
        }
    }
    if ($dependencias.Count -gt 0) {
        $dependencyGraph[$nomeSemExtensao] = $dependencias
    }

    # --- TAGS SEMANTICAS, TECNOLOGIAS E PADROES ---
    $tags = [System.Collections.Generic.List[string]]::new()
    $techs = [System.Collections.Generic.List[string]]::new()
    $patterns = [System.Collections.Generic.List[string]]::new()
    
    if ($conteudo -match "HttpClient") { $tags.Add("[HTTP]"); $techs.Add("HttpClient") }
    if ($conteudo -match "Kernel") { $tags.Add("[SEMANTIC_KERNEL]"); $techs.Add("Semantic Kernel") }
    if ($conteudo -match "AngleSharp") { $tags.Add("[SCRAPING]"); $techs.Add("AngleSharp") }
    if ($conteudo -match "DbContext|DbSet") { $tags.Add("[DATABASE]"); $techs.Add("Entity Framework") }
    if ($conteudo -match "Task\.WhenAll|Task\.Run") { $tags.Add("[PARALLELISM]") }
    if ($conteudo -match "async\s+Task") { $tags.Add("[ASYNC]") }
    
    if ($tipo -eq "Controller") { $patterns.Add("MVC/API Controller") }
    if ($conteudo -match "AddScoped|AddTransient|AddSingleton") { $patterns.Add("Dependency Injection") }
    if ($nomeSemExtensao -match "Factory$") { $patterns.Add("Factory Pattern") }

    # --- DETECCAO DE ENDPOINTS (SE CONTROLLER) ---
    $endpoints = @()
    if ($tipo -eq "Controller") {
        $linhas = $conteudo -split "`n"
        foreach ($linha in $linhas) {
            if ($linha -match "\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)") {
                $endpoints += $linha.Trim()
            }
        }
    }

    # --- ADVERTENCIAS DE RISCO ---
    $warnings = @()
    if ($conteudo -match "http://") { $warnings += "Uso de protocolo HTTP inseguro" }
    if ($conteudo -match "Task\.WhenAll" -and $conteudo -notmatch "CancellationToken") { $warnings += "Task.WhenAll sem CancellationToken detectado" }
    if ($conteudo -match "password=|secret=|apiKey=") { $warnings += "Possivel credencial Hardcoded" }

    # --- METRICAS E COMPACTACAO DE CODIGO ---
    $linhasBrutas = $conteudo -split "`n"
    $metodoCount = ([regex]::Matches($conteudo, "(public|private|internal|protected)\s+")).Count
    $asyncMetodoCount = ([regex]::Matches($conteudo, "async\s+Task")).Count

    # Limpeza de Codigo
    $conteudoLimpo = [regex]::Replace($conteudo, "///\s*<.*?>", "")
    $conteudoLimpo = [regex]::Replace($conteudoLimpo, "(?s)/\*.*?\*/", "")
    
    $usings = [System.Collections.Generic.List[string]]::new()
    $linhasLimpas = [System.Collections.Generic.List[string]]::new()
    
    foreach ($line in ($conteudoLimpo -split "`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -match "^using\s+([^;]+);") {
            $usings.Add($Matches[1])
        } elseif (-not [string]::IsNullOrWhiteSpace($trimmed)) {
            $linhasLimpas.Add($line.TrimEnd())
        }
    }
    
    $codigoFinal = $linhasLimpas -join "`n"
    $codigoFinal = [regex]::Replace($codigoFinal, "(?m)^\s*`n", "")

    # Escrever metadados no arquivo de arquitetura
    $arquiteturaConsolidada.AppendLine("--------------------------------------------------")
    $arquiteturaConsolidada.AppendLine("ARQUIVO: $caminhoRelativo")
    $arquiteturaConsolidada.AppendLine("TYPE: $tipo")
    $arquiteturaConsolidada.AppendLine("NAMESPACE: $namespace")
    if ($dependencias.Count -gt 0) { $arquiteturaConsolidada.AppendLine("DEPENDENCIES: " + ($dependencias -join ", ")) }
    if ($tags.Count -gt 0) { $arquiteturaConsolidada.AppendLine("TAGS: " + ($tags -join " ")) }
    if ($techs.Count -gt 0) { $arquiteturaConsolidada.AppendLine("TECH: " + ($techs -join ", ")) }
    if ($patterns.Count -gt 0) { $arquiteturaConsolidada.AppendLine("PATTERNS: " + ($patterns -join ", ")) }
    if ($endpoints.Count -gt 0) { $arquiteturaConsolidada.AppendLine("ENDPOINTS:`n  - " + ($endpoints -join "`n  - ")) }
    if ($warnings.Count -gt 0) { $arquiteturaConsolidada.AppendLine("WARNINGS: " + ($warnings -join " | ")) }
    $arquiteturaConsolidada.AppendLine("METRICS: Linhas Brutas: $($linhasBrutas.Length) | Metodos Estimados: $metodoCount | Async: $asyncMetodoCount")
    $arquiteturaConsolidada.AppendLine("--------------------------------------------------`n")

    # Escrever codigo limpo mapeado
    $codigoConsolidado.AppendLine("==================================================")
    $codigoConsolidado.AppendLine("CONTEUDO COMPACTADO: $caminhoRelativo")
    $codigoConsolidado.AppendLine("USINGS: " + ($usings -join ", "))
    $codigoConsolidado.AppendLine("==================================================")
    $codigoConsolidado.AppendLine($codigoFinal)
    $codigoConsolidado.AppendLine("`n")
}

# --- GERAR INDICE GLOBAL E GRAFO NO TOPO ---
$relatorioEstrutura = [System.Text.StringBuilder]::new()
$relatorioEstrutura.AppendLine("==================================================")
$relatorioEstrutura.AppendLine("INDICE GLOBAL DO PROJETO")
$relatorioEstrutura.AppendLine("==================================================")
foreach ($chave in $indiceGlobal.Keys) {
    $relatorioEstrutura.AppendLine("[$chave]")
    foreach ($item in $indiceGlobal[$chave]) {
        $relatorioEstrutura.AppendLine("  - $item")
    }
}
$relatorioEstrutura.AppendLine("`n==================================================")
$relatorioEstrutura.AppendLine("MAPA DE DEPENDENCIAS (CONSTRUTORES)")
$relatorioEstrutura.AppendLine("==================================================")
foreach ($chave in $dependencyGraph.Keys) {
    $relatorioEstrutura.AppendLine("$chave")
    foreach ($dep in $dependencyGraph[$chave]) {
        $relatorioEstrutura.AppendLine("  -> $dep")
    }
}
$relatorioEstrutura.AppendLine("`n==================================================")
$relatorioEstrutura.AppendLine("FLUXO DE CHAMADAS TEORICO (SUGESTAO DE EXECUCAO)")
$relatorioEstrutura.AppendLine("==================================================")
$relatorioEstrutura.AppendLine("Controller -> Service/Handler -> Repository/Scraper/AI_Filter -> External_Response`n")

# Salvar Arquivos na pasta local do script
$relatorioCompletoArquitetura = $relatorioEstrutura.ToString() + $arquiteturaConsolidada.ToString()
Set-Content -Path $arquivosArquitetura -Value $relatorioCompletoArquitetura -Encoding UTF8
Set-Content -Path $arquivoCodigo -Value $codigoConsolidado.ToString() -Encoding UTF8

Write-Host "Processo concluido com sucesso!" -ForegroundColor Green
Write-Host "Analise realizada na raiz: $diretorioRaizProjeto" -ForegroundColor Yellow
Write-Host "Arquivos gerados/atualizados em: $diretorioDoScript" -ForegroundColor Cyan