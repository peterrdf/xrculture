// Third-part tools

g_getObjectDescription = function (object) {
  if (object &&
    (object.label !== undefined)
    && (object.label !== null)
    && (object.label !== '')) {
    return object.label;
  }

  return null;
}

if (embeddedMode()) {
  hideUI();

  window.onmessage = function (e) {
    try {
      let event = null;

      try {
        event = JSON.parse(e.data);
      }
      catch (ex) {
        console.error(ex);
      }

      if (event) {
        switch (event.type) {
          case 'loadFile': {
            loadFile(event.file);
          }
            break;

          case 'loadFileByPath': {
            loadFileByPath(event.file);
          }
            break;

          case 'loadFileByUri': {
            loadFileByUri(event.file);
          }
            break;

          case 'loadFileContent': {
            loadContent(event.name, event.fileExtension, event.content);

            let completedEvent = {
              'type': 'loadContent',
              'name': event.name
            };
            e.source.postMessage(JSON.stringify(completedEvent), '*');
          }
            break;

          case 'addFileContent': {
            addContent(event.name, event.fileExtension, event.content);

            let completedEvent = {
              'type': 'loadContent',
              'name': event.name
            };
            e.source.postMessage(JSON.stringify(completedEvent), '*');
          }
            break;
        }
      }
    }
    catch (ex) {
      console.error(ex);
    }
  };
}