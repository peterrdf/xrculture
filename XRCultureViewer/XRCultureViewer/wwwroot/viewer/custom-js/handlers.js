/*
* Mouse/touch support
*/
const SELECT_MODE = 0;
const ZOOM_MODE = 1;
const ROTATE_MODE = 2;
const PAN_MODE = 3;
var g_interactionMode = SELECT_MODE;
var g_interactionMoveInProgress = false; // Mouse/touch
var g_startMouseX = -1;
var g_startmouseY = -1;
var g_mouseX = -1;
var g_mouseY = -1;
var g_zoomStartX = -1;
var g_zoomStartY = -1;
var g_touchesDistance = -1;
var g_oldEyeVector = null;

var g_onRunWebGLAppEvent = null;

/**
 * Event handler - optional
 */
function onWindowResize() {
    resizeCanvas($('#canvas_container').width(), $('#canvas_container').height());
}

/**
 * Event handler - optional
 */
function onViewerCheckServerStatus() { }

/**
 * Event handler - optional
 */
function onViewerRender() { }

/**
 * Event handler - optional
 */
function onViewerDynamicContentLoaded() { }

/**
 * Executes the WebGL application.
 */
function runWebGLApp() {
    /**
     * Update UI
     */

    /**
     * Init IFCViewer
     */
    g_viewer.init('canvas-element-id', $('#canvas_container').width(), $('#canvas_container').height());

    if (g_onRunWebGLAppEvent !== null) {
        g_onRunWebGLAppEvent();
    }
}

// https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API/File_drag_and_drop
function dragOverHandler(ev) {
    console.log('File(s) in drop zone');

    // Prevent default behavior (Prevent file from being opened)
    ev.preventDefault();
}

function dropHandler(ev) {
    console.log('File(s) dropped');

    // Prevent default behavior (Prevent file from being opened)
    ev.preventDefault();

    if (ev.dataTransfer.items) {
        // Use DataTransferItemList interface to access the file(s)
        if (ev.dataTransfer.items.length > 0) {
            loadFile(ev.dataTransfer.items[0].getAsFile());
        }
    } else {
        // Use DataTransfer interface to access the file(s)
        if (ev.dataTransfer.files.length > 0) {
            loadFile(ev.dataTransfer.files[0]);
        }
    }
}