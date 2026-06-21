# DInject Benchmarks

Сравнение скорости DI-контейнеров: **DInject** (этот пакет, codegen) vs **VContainer**, **Reflex**, **Extenject**.

Живёт в `Assets/` (dev-харнесс), **не в пакете** — деливерабл `com.draasgames.dinject` не затрагивается и в UPM не попадает.

## Что уже работает (MVP)

- Сценарий `Transient.DeepWide`, фаза **warm** (установившийся резолв), метрики: время (µs, median/stdev/min/max) + GC-аллокации.
- Контейнер DInject — собирается и гоняется **сразу**, без установки чего-либо.
- Конкуренты — заготовлены, но выключены (см. ниже).

## Как запустить

1. Открыть проект в Unity.
2. `Window > General > Test Runner` → вкладка **PlayMode**.
3. Запустить `DInjectBench > ResolveBenchmarks > ResolveWarm`.
4. Результаты — в `Window > Analysis > Performance Test Report`, группы вида `DInject.Transient.DeepWide.ResolveWarm`.

> ⚠️ Это цифры из редактора (**Mono/JIT**) — годятся для относительной итерации, но **не для публикации**. Заголовочные числа снимаются с **IL2CPP**-билда на реальном устройстве (см. «IL2CPP»). NB: в `TestResults.xml` поле `ScriptingBackend` показывает настройку проекта, а `Platform: WindowsEditor` — реальную среду прогона; PlayMode-тесты из Test Runner всегда идут на Mono в редакторе.

### Как читать цифры

- **Время:** при заданном `MeasurementCount` сэмпл — это сумма по `IterationsPerMeasurement` (1000). Время одного резолва = `Median / 1000`. (Пример: median 41520 µs ⇒ ≈ 41.5 µs/резолв.)
- **GC (`...ResolveWarm.GC()`):** уже на один резолв (фреймворк делит на итерации) — читается как есть (кол-во GC.Alloc-операций).

Если DInject-кейс уходит в Ignore с сообщением про генератор — значит `CodeGen/DInject.CodeGen.dll` не импортирован с лейблом `RoslynAnalyzer` (рефлексионного фоллбэка нет).

## Архитектура

```
Assets/Benchmarks/
  Shared/      контракты графа (ILeaf/IMid/IServiceGraphRoot) + IContainerAdapter — без ссылок на контейнеры
  DInject/     граф (partial + [Inject] ctor → codegen) + DInjectAdapter   [активен]
  Runner/      ResolveBenchmarks ([Test, Performance]) + BenchAdapters (поиск адаптеров рефлексией)
  Competitors/
    VContainer/  граф + адаптер   [выключен]
    Reflex/      граф + адаптер   [выключен]
    Zenject/     граф + адаптер   [выключен]  (Extenject)
```

- **Runner не ссылается ни на один контейнер.** Адаптеры находятся рефлексией (`BenchAdapters`), поэтому добавление/удаление контейнера не требует правок Runner.
- **Каждый контейнер изолирован в своей сборке** со своей копией графа — атрибуты/кодоген разных контейнеров не пересекаются. У Zenject и DInject одинаковые имена типов (`DiContainer`, `[Inject]`) — поэтому они в разных сборках с разными `using`.

## Конкуренты (установлены)

Все три стоят в `Packages/manifest.json` (OpenUPM) и гейтятся **только наличием пакета** (`versionDefines` → `HAS_*` в их `.asmdef`). Удалишь пакет — сборка адаптера тихо выключится, проект не сломается.

| Контейнер | UPM id / версия | Имя asmdef | Fast-path |
|---|---|---|---|
| VContainer | `jp.hadashikick.vcontainer` 1.18.0 | `VContainer` | Source Generator (по умолчанию) |
| Reflex | `com.gustavopsantos.reflex` 14.3.1 | `Reflex` | expression-деревья (Mono) / AOT (IL2CPP) |
| Extenject | `com.svermeulen.extenject` 9.2.0-stcf3 | `Zenject` | Reflection Baking (по умолчанию — рефлексия) |

> ⚠️ **Extenject и DInject делят .meta GUID (наследие форка).** Чтобы они сосуществовали в одном проекте, 8 GUID в пакете DInject (`SceneContext`, `ProjectContext`, `GameObjectContext`, `SceneDecoratorContext`, `ZenjectBinding`, `ZenAutoInjecter`, `DefaultSceneContractConfig`, `SceneTestFixtureSceneReference`) сменены на уникальные, а все ссылки в сценах/префабах пакета обновлены атомарно. Без этого Unity выкидывает дубли и Extenject не компилируется.

## Честность сравнения (кратко)

- Сравнивай **fast-path с fast-path**: DInject codegen ↔ VContainer SG ↔ Extenject baked. Extenject имеет смысл гонять и baked, и unbaked — отдельными метками.
- Одинаковые lifetime (везде transient), одинаковый граф, одинаковый managed-stripping уровень.
- Меряй фазы раздельно: Build / cold first-resolve / warm. Не суммировать.
- GC-байты — равноправная метрика со временем.

## Дальше (не в MVP)

- Фазы `Build` и `ResolveCold`; реальные байты/оп через `Measure.Custom` + `GC.GetTotalAllocatedBytes(true)`.
- Сценарии: `Singleton`, `Factory.Spawn` (1k–10k в цикле), `MonoInject` (PlayMode `[UnityTest]`), `OpenGeneric`.
- Прогон на устройстве под **IL2CPP** (Test Runner → PlayMode → Run Location), ≥3 перезапуска, фикс quality/vSync/частоты.
