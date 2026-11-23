# RC Drift Dash

Стартовый проект автономной MR-игры для Meta Quest 3S. Открывайте корень репозитория в Unity 6000.3.7f1. В Assets/Scenes/QuestCheck.unity находится проверочная сцена. В Play она показывает макет размером 1 × 1 м и красную модель для оценки масштаба. На шлеме макет устанавливается перед игроком один раз при получении позы головы. Осмотритесь и наклонитесь к макету, чтобы проверить трекинг. Passthrough, размещение на реальном столе, управление машиной и заезд относятся к следующим этапам.

## Первый запуск на Quest 3S

1. В Unity Hub откройте Installs → 6000.3.7f1 → Add modules. Установите Android Build Support, Android SDK & NDK Tools и OpenJDK.
2. Добавьте папку проекта через Projects → Add. Откройте сцену QuestCheck. В File → Build Profiles выберите Android и нажмите Switch Platform.
3. Включите Developer Mode для гарнитуры в приложении Meta Horizon. Подключите включённый шлем к ПК кабелем USB с передачей данных и разрешите USB-отладку в гарнитуре.
4. В Android Build Profile выберите шлем в Run Device и нажмите Build And Run. Сохраните APK в Builds/RCDriftDash.apk. Первичная сборка использует локальную отладочную подпись.
5. В шлеме проверьте стереоизображение, масштаб диорамы и её неподвижность при повороте и перемещении головы.

В проекте настроены Android ARM64, IL2CPP, OpenXR, Meta Quest Support и профили контроллеров Touch. Android application ID — com.rcdriftdash.prototype.

## Подключение Git

Создайте пустой удалённый репозиторий на GitHub или GitLab. Откройте PowerShell в папке RCDriftDash, замените URL своим и выполните команды.

```powershell
git remote add origin <URL>
git push -u origin main
```

Unity сохраняет ресурсы в текстовом формате и отслеживает .meta. Кэши, сборки и ключи подписи исключены из Git.
