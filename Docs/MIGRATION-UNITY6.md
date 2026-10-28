# Переезд на Unity 6

Проект написан под **2021.3.11f1**. Ставим актуальную версию Unity 6 (LTS), поэтому переезд — не «открыть и согласиться на апгрейд», а осознанный перенос.

## Почему не открывать старый проект напрямую

При открытии 2021-проекта в Unity 6 редактор поднимет пакеты и перепишет `ProjectSettings`. Проблемы:

- **Cinemachine** 2.8.9 несовместим с Unity 6: нужен **Cinemachine 3.x**, где API переименован (`CinemachineFreeLook` → `CinemachineCamera` + `CinemachineOrbitalFollow`). Сцены с FreeLook-ригами потеряют компоненты.
- **TextMesh Pro** 3.0.6 в Unity 6 входит в `com.unity.ugui` 2.0. Пакет TMP как отдельная зависимость конфликтует.
- **Input Manager**: Unity 6 по умолчанию создаёт проекты с Input System Package. Старый код на `Input.GetAxis` перестанет компилироваться/работать.

Сцены у нас всё равно пустые (`Level1` — камера и свет), а скрипты придётся переписывать. Значит, чище собрать новый проект и перенести в него ассеты.

## План переноса

1. **Создать проект Unity 6** — шаблон **3D (Built-In Render Pipeline)**, имя `InvaderOfWorlds6`.
   - Если в Hub такого шаблона нет — берём **Universal 3D (URP)** и прогоняем материалы через *Window → Rendering → Render Pipeline Converter*.
   - Built-in предпочтительнее: все ассеты (`LowPolyAlienWorld`, `polyperfect`) сделаны под него и в URP станут розовыми, пока не сконвертируешь.
2. **Скопировать ассеты с `.meta`.** Папки `Additions Assets`, `3D Models`, `Prefabs` — целиком, вместе с `.meta`: GUID сохранятся, ссылки внутри префабов не порвутся.
3. **Не копировать** `Assets/TextMesh Pro` (Unity 6 ставит свой) и `Assets/Scenes` (пересоберём).
4. **Скрипты** — переносить по одному, переписывая под новые API. Старые версии остаются в git-истории, ничего не теряется.
5. **Настройки проекта**: Active Input Handling → *Both* (на время перехода), теги и слои перенести вручную, если понадобятся.
6. **Пакеты**: `com.unity.cinemachine` 3.x, `com.unity.inputsystem` (если решим уходить с legacy), `com.unity.ugui` 2.x.

## Что это значит для кода

| Было (2021) | Стало (Unity 6) |
|---|---|
| `CinemachineFreeLook`, `CinemachineVirtualCamera` | `CinemachineCamera` + `CinemachineOrbitalFollow` / свой риг |
| `TextMeshProUGUI` из пакета TMP | тот же класс, но из `com.unity.ugui` 2.0 |
| `Input.GetAxis("Horizontal")` | либо legacy (Active Input Handling = Both), либо `InputAction` |
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` (старый вариант помечен устаревшим) |

## Порядок, когда редактор установится

- [ ] Уточнить точную версию (6.0 / 6.1 / 6.2 / 6.3)
- [ ] Создать проект и убедиться, что пустая сцена запускается
- [ ] Скопировать ассеты, проверить, что префабы открываются и материалы не розовые
- [ ] Перенести `PlanetGravity`, `PlanetWalker`, камерный риг (новый код)
- [ ] Первый коммит в ветку `feature/planet-movement`
