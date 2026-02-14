Photos Feature

PhotoPickerView: ссылки на `ValueSelectModalPanel`, `ValueUploadButton`, `ValueSelectCloseButton`, `Content`, `PhotoItemPrefab`.
PhotoPickerController: загрузка списка `/images`, предпросмотр через RawImage, выбор фото (событие `OnPhotoChosen(fileName, tex)`), загрузка локального файла через SimpleFileBrowser (если включён `SIMPLE_FILE_BROWSER`).

Интеграция с FieldRenderer: подписаться на `fieldRenderer.OnSelectImageRequested += fieldName => { picker.Open(); picker.OnPhotoChosen = (fileName, tex) => { fieldRenderer.ApplyPickedImage(fieldName, fileName, tex); } }`.


