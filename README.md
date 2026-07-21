<div align="center">

# DeepTools

**Smart tweaker, cleaner and monitoring tool for Windows — in one app.**
**Умный твикер, чистильщик и монитор для Windows — всё в одном.**

[![Version](https://img.shields.io/badge/version-1.0.0-blue)](https://github.com/dep1xar/depixclicker/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6)](#)
[![.NET](https://img.shields.io/badge/.NET%20Framework-4.x-512BD4)](#)

**[English](#english) · [Русский](#русский)**

</div>

---

<a name="english"></a>

## English

DeepTools is a lightweight all-in-one utility for Windows 10 and 11. It cleans junk, tunes the system for gaming, watches your temperatures, manages startup and services, removes pre-installed bloat, and bundles a few handy extras like a clicker, screenshot tool and clipboard manager. One executable, no installer, no background services left behind.

> [!IMPORTANT]
> DeepTools requires **administrator rights** to change power plans, services and startup entries, and to read hardware sensors. On launch it asks for elevation via the standard Windows UAC prompt.

### Why it may be flagged by antivirus

DeepTools is a system tweaker, so it does things real malware also does: elevates via UAC, edits the registry and services, sends synthetic input (the clicker) and loads a kernel driver to read CPU/GPU temperatures. Because the release is **not code-signed yet**, some engines (mostly ML/heuristic detections like `Trojan:Win32/Wacatac.B!ml`) may show a false positive. This is expected for this class of software.

- The full source is in this repository — you can read and build it yourself.
- You can verify any release on [VirusTotal](https://www.virustotal.com/).
- If Windows SmartScreen warns you, click **More info → Run anyway**.

### Features

| Section | What it does |
|---|---|
| **Home** | Quick tiles to jump to the most-used tools. |
| **Smart Cleanup** | Clears temp files, caches and other junk by category, with a size preview before you delete. |
| **Game Booster** | One-click "Ultimate Performance" power plan, disables core parking, boosts the running game's priority, optional auto-boost for new games, FPS overlay and Win-key blocking. |
| **Health Check** | Live CPU/GPU/RAM load and temperatures, disk health (SMART), and per-component status. |
| **My PC** | Full system spec on one page with a **Copy** button — handy for forums, sales listings or sending to a friend. |
| **Startup** | See and toggle what launches with Windows. |
| **Services** | Enable/disable Windows services safely, with descriptions. |
| **Visual Effects** | Toggle Windows animations and effects for a snappier feel. |
| **Clicker** | Configurable auto-clicker with hotkey. |
| **Screenshots** | Region capture with a global hotkey (default **F9**). |
| **Clipboard** | Clipboard history manager. |

### Extra tools

- **Debloat** — removes pre-installed UWP apps you don't use. Only shows what's actually installed; removal is per-user and always confirmed.
- **Benchmark** — quick before/after CPU, RAM and disk test. Saves the result so you can see if your tweaks actually helped.
- **Stress Test** — loads all CPU cores to 100% for 2 minutes and reports peak temperature — shows whether your cooler can keep up.
- **Temperature History** — 24-hour CPU/GPU graph with peak analysis.
- **Game Time** — tracks your gaming sessions (playtime, average CPU, peak temps) and reports when you close a game.
- **Crosshair** — customizable on-screen crosshair overlay (cross, dot, circle, T-shape).
- **Desktop Widget** — compact always-on-top CPU/RAM/temperature panel.

### Installation

1. Go to the [Releases](https://github.com/dep1xar/depixclicker/releases) page.
2. Download the latest `DeepTools.exe`.
3. Run it. Approve the UAC prompt when Windows asks for administrator rights.

No installer, no dependencies to download — all libraries (including the hardware-sensor engine) are embedded into the single `.exe`.

### Building from source

You need the **.NET Framework 4.x** SDK (ships with Windows / Visual Studio; the compiler lives at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`).

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

The script compiles all `.cs` files, embeds the managed DLLs as resources and produces `DeepTools.exe` in the project folder.

### System requirements

- Windows 10 or Windows 11 (64-bit)
- .NET Framework 4.x (preinstalled on Windows 10/11)
- Administrator rights (for tweaks and sensors)

### Language

DeepTools ships in **English and Russian**. On first launch it picks the language from your Windows locale; you can change it any time in **Settings → Appearance & language**.

---

<a name="русский"></a>

## Русский

DeepTools — это лёгкая утилита «всё в одном» для Windows 10 и 11. Она чистит мусор, настраивает систему под игры, следит за температурами, управляет автозагрузкой и службами, удаляет предустановленный хлам и включает несколько удобных инструментов: кликер, скриншотер и менеджер буфера обмена. Один исполняемый файл, без установщика и без фоновых служб, остающихся в системе.

> [!IMPORTANT]
> DeepTools нужны **права администратора**, чтобы менять планы питания, службы и автозагрузку, а также читать датчики железа. При запуске программа запрашивает их через стандартное окно UAC.

### Почему антивирус может ругаться

DeepTools — системный твикер, поэтому он делает то же, что и настоящие вирусы: повышает права через UAC, правит реестр и службы, шлёт синтетический ввод (кликер) и грузит драйвер ядра для чтения температур CPU/GPU. Так как релиз **пока не подписан сертификатом**, некоторые движки (в основном ML/эвристика вроде `Trojan:Win32/Wacatac.B!ml`) могут дать ложное срабатывание. Для такого класса программ это нормально.

- Весь исходный код — в этом репозитории, можно прочитать и собрать самому.
- Любой релиз можно проверить на [VirusTotal](https://www.virustotal.com/).
- Если предупреждает Windows SmartScreen — нажми **Подробнее → Выполнить в любом случае**.

### Возможности

| Раздел | Что делает |
|---|---|
| **Главная** | Плитки быстрого перехода к самым нужным инструментам. |
| **Умная очистка** | Чистит временные файлы, кэши и прочий мусор по категориям, показывает размер до удаления. |
| **Игровой буст** | В один клик включает план «Максимальная производительность», отключает парковку ядер, поднимает приоритет запущенной игры, опционально авто-буст новых игр, FPS-оверлей и блокировку клавиши Win. |
| **Проверка здоровья** | Нагрузка и температуры CPU/GPU/RAM в реальном времени, здоровье диска (SMART), статус по компонентам. |
| **Мой ПК** | Полная спека системы на одной странице с кнопкой **Копировать** — удобно для форумов, объявлений или отправки другу. |
| **Автозагрузка** | Смотри и отключай то, что стартует вместе с Windows. |
| **Службы** | Безопасно включай/отключай службы Windows, с описаниями. |
| **Визуальные эффекты** | Отключай анимации и эффекты Windows для отзывчивости. |
| **Кликер** | Настраиваемый авто-кликер с горячей клавишей. |
| **Скриншоты** | Захват области по глобальной горячей клавише (по умолчанию **F9**). |
| **Буфер обмена** | Менеджер истории буфера обмена. |

### Дополнительные инструменты

- **Деблоат** — удаляет предустановленные UWP-приложения, которыми ты не пользуешься. Показывает только реально установленное; удаление только для текущего пользователя и всегда с подтверждением.
- **Бенчмарк** — быстрый тест CPU, RAM и диска «до/после». Сохраняет результат, чтобы увидеть, дали ли твики реальный прирост.
- **Стресс-тест** — грузит все ядра CPU на 100% в течение 2 минут и показывает пиковую температуру — видно, вывозит ли кулер.
- **История температур** — график CPU/GPU за 24 часа с разбором пиков.
- **Время в играх** — считает игровые сессии (время игры, средний CPU, пиковые температуры) и шлёт отчёт при закрытии игры.
- **Прицел** — настраиваемый накладной прицел (крест, точка, круг, T-образный).
- **Виджет на рабочий стол** — компактная плашка CPU/RAM/температуры, всегда поверх окон.

### Установка

1. Открой страницу [Releases](https://github.com/dep1xar/depixclicker/releases).
2. Скачай свежий `DeepTools.exe`.
3. Запусти. Подтверди запрос UAC, когда Windows попросит права администратора.

Без установщика и без докачки зависимостей — все библиотеки (включая движок датчиков) встроены прямо в один `.exe`.

### Сборка из исходников

Нужен **.NET Framework 4.x** SDK (идёт с Windows / Visual Studio; компилятор лежит по пути `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`).

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Скрипт компилирует все `.cs`-файлы, встраивает managed-библиотеки как ресурсы и создаёт `DeepTools.exe` в папке проекта.

### Системные требования

- Windows 10 или Windows 11 (64-бит)
- .NET Framework 4.x (предустановлен в Windows 10/11)
- Права администратора (для твиков и датчиков)

### Язык

DeepTools доступен на **русском и английском**. При первом запуске язык выбирается по локали Windows; сменить можно в любой момент в **Настройки → Внешний вид и язык**.

---

<div align="center">

© 2026 dep1xar · DeepTools v1.0.0

</div>

