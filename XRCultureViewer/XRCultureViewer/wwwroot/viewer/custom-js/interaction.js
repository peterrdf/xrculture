const ACTION_HIDE = 'hide'
const ACTION_SHOW = 'show'
let leftSidebarWidth, rightSidebarWidth

/*
 * Interaction support
 */
var g_zoomTimeoutID = 0
var g_eyeVector = null;
function zoom(zoomIn) {
  try {
    if (!g_interactionMoveInProgress) {
      return
    }

    var zoomFactor = zoomIn
      ? g_viewer._worldDimensions.MaxDistance * 0.01
      : -(g_viewer._worldDimensions.MaxDistance * 0.01)

    var near = [0, 0, 0, 0]
    var far = [0, 0, 0, 0]
    var dir = [0, 0, 0]

    var X = (g_zoomStartX - gl.canvas.width / 2.0) / (gl.canvas.width / 2.0)
    var Y = -(g_zoomStartY - gl.canvas.height / 2.0) / (gl.canvas.height / 2.0)

    mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, -1.0, 1.0], near)
    vec3.scale(near, 1 / near[3])

    mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, 0.0, 1.0], far)
    vec3.scale(far, 1 / far[3])

    // calculate world space view vector
    vec3.subtract(far, near, dir)
    vec3.normalize(dir)
    vec3.scale(dir, zoomFactor)

    // move eye in direction of world space view vector
    if (g_eyeVector === null) {
      g_eyeVector = vec3.create(g_viewer._eyeVector)
    }
    vec3.subtract(g_eyeVector, dir)

    if (g_zoomTimeoutID !== 0) {
      clearTimeout(g_zoomTimeoutID)
      g_zoomTimeoutID = 0
    }

    g_zoomTimeoutID = setTimeout(() => {
      g_viewer._eyeVector = vec3.create(g_eyeVector)
      g_eyeVector = null;

      PENDING_DRAW_SCENE = true
    }, 10)
  } catch (ex) {
    console.error(ex)
  }
}

/*
 * Interaction support
 */
function mouseWheelZoom(zoomIn, zoomX, zoomY, speed) {
  try {
    var zoomFactor = zoomIn ? 0.005 * speed : -0.005 * speed

    var near = [0, 0, 0, 0]
    var far = [0, 0, 0, 0]
    var dir = [0, 0, 0]

    var X = (zoomX - gl.canvas.width / 2.0) / (gl.canvas.width / 2.0)
    var Y = -(zoomY - gl.canvas.height / 2.0) / (gl.canvas.height / 2.0)

    mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, -1.0, 1.0], near)
    vec3.scale(near, 1 / near[3])

    mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, 0.0, 1.0], far)
    vec3.scale(far, 1 / far[3])

    // calculate world space view vector
    vec3.subtract(far, near, dir)
    vec3.normalize(dir)
    vec3.scale(dir, zoomFactor)

    // move eye in direction of world space view vector
    vec3.subtract(g_viewer._eyeVector, dir)
  } catch (ex) {
    console.error(ex)
  }

  PENDING_DRAW_SCENE = true
}

/*
 * Interaction support
 */
function rotate(x, y) {
  if (!g_interactionMoveInProgress) {
    return
  }

  if (g_mouseX === -1 || g_mouseY === -1) {
    return
  }

  // Rotate by X
  var rotateX = ((y - g_mouseY) / gl.canvas.height) * 360
  g_viewer._rotateX += rotateX

  // Rotate by Y
  var rotateY = ((x - g_mouseX) / gl.canvas.width) * 360
  g_viewer._rotateY += rotateY

  PENDING_DRAW_SCENE = true
}

/*
 * Interaction support
 */
function pan(x, y) {
  if (!g_interactionMoveInProgress) {
    return
  }

  var near = [0, 0, 0, 0]
  var far = [0, 0, 0, 0]
  var dir = [0, 0, 0]

  var X = (x - gl.canvas.width / 2.0) / (gl.canvas.width / 2.0)
  var Y = -(y - gl.canvas.height / 2.0) / (gl.canvas.height / 2.0)

  mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, -1.0, 1.0], near)
  vec3.scale(near, 1 / near[3])

  mat4.multiplyVec4(g_viewer._mtxInversePMV, [X, Y, 0.0, 1.0], far)
  vec3.scale(far, 1 / far[3])

  // calculate world space view vector
  vec3.subtract(far, near, dir)
  vec3.normalize(dir)
  vec3.scale(dir, g_viewer._eyeVector[2])

  if (g_oldEyeVector != null) {
    // move eye in direction of world space view vector
    var XDiff = dir[0] - g_oldEyeVector[0]
    var YDiff = dir[1] - g_oldEyeVector[1]

    g_viewer._eyeVector[0] -= XDiff
    g_viewer._eyeVector[1] -= YDiff
  }

  g_oldEyeVector = dir

  PENDING_DRAW_SCENE = true
}

/*
 * Helper
 */
function resetInteractionData() {
  g_interactionMode = SELECT_MODE
  g_interactionMoveInProgress = false
  g_startMouseX = -1
  g_startMouseY = -1
  g_mouseX = -1
  g_mouseY = -1
  g_zoomStartX = -1
  g_zoomStartY = -1
  g_touchesDistance = -1
  g_oldEyeVector = null

  PENDING_DRAW_SCENE = true
}

/*
 * Event handler
 */
window.addEventListener(
  'load',
  function (event) {
    /*
     * Event handler
     */
    document.body.addEventListener(
      'touchstart',
      function (event) {
        try {
          resetInteractionData()

          g_interactionMoveInProgress = true

          if (event.target instanceof HTMLCanvasElement) {
            event.preventDefault()

            /*
             * Pick
             */
            var x = event.touches[0].pageX
            var y = event.touches[0].pageY

            g_viewer.pickObject(x, y)
          }

          g_startMouseX = event.touches[0].pageX
          g_startMouseY = event.touches[0].pageY
          g_mouseX = event.touches[0].pageX
          g_mouseY = event.touches[0].pageY
        } catch (ex) {
          console.error(ex)
        }
      },
      false
    )

    /*
     * Event handler
     */
    document.body.addEventListener(
      'touchmove',
      function (event) {
        try {
          if (event.target instanceof HTMLCanvasElement) {
            event.preventDefault()
          }

          if (event.touches.length >= 2) {
            /*
             * Zoom/Pan
             */
            var touchesDistance = Math.sqrt(
              Math.pow(event.touches[1].pageX - event.touches[0].pageX, 2) +
                Math.pow(event.touches[1].pageY - event.touches[0].pageY, 2)
            )

            if (g_touchesDistance != -1) {
              if (Math.abs(touchesDistance - g_touchesDistance) >= 10) {
                var zoomIn = touchesDistance > g_touchesDistance ? true : false
                zoom(zoomIn)
              } else {
                pan(
                  (event.touches[0].pageX + event.touches[1].pageX) / 2,
                  (event.touches[0].pageY + event.touches[1].pageY) / 2
                )
              }
            } // if (g_touchesDistance != -1)

            g_touchesDistance = touchesDistance
          } // if (event.touches.length >= 2)
          else {
            /*
             * Rotate
             */
            rotate(event.touches[0].pageX, event.touches[0].pageY)
          } // else if (event.touches.length >= 2)

          g_mouseX = event.touches[0].pageX
          g_mouseY = event.touches[0].pageY
        } catch (ex) {
          console.error(ex)
        }
      },
      false
    )

    /*
     * Event handler
     */
    document.body.addEventListener(
      'touchend',
      function (event) {
        resetInteractionData()
      },
      false
    )

    /*
     * Event handler
     */
    document.body.addEventListener(
      'touchcancel',
      function (event) {
        resetInteractionData()
      },
      false
    )

    $('#zoom-nav').click(function (e) {
      setView(e)
    })
  },
  false
)

window.onresize = onWindowSizeHandler

function onWindowSizeHandler() {
}

const toggleAllSets = (container, action) => {
  const allSetsSelector = $(`${container} .collapse`)

  allSetsSelector.each(function () {
    $(this).collapse(action)
  })

  const allHeaderSelector = $(`${container} .arrow`)

  allHeaderSelector.each(function () {
    if (action === 'hide') {
      $(this).addClass('collapsed')
    } else {
      $(this).removeClass('collapsed')
    }
  })
}

/*
 * Event handler
 */
$('#canvas-element-id').mousedown(function (event) {
  try {
    resetInteractionData()

    switch (event.which) {
      case 1:
        {
          g_interactionMode = ROTATE_MODE

          /*
           * URI
           */
          try {
            parent.clearSearch()
          } catch (e) {}
          if (
            g_viewer._pickedObject != -1 &&
            g_instances[g_viewer._pickedObject - 1].uri != undefined &&
            g_groups.length > 0
          ) {
            Cookies.set('selURI', g_instances[g_viewer._pickedObject - 1].uri)
            if (window.top == window.self) {
              $('#button-run').find('a').text('Select')
            } else {
              $('#button-run').find('a').text('Metadata')
            }
            $('#button-run').attr('data-uri', Cookies.get('selURI'))
            $('#button-run').click(function () {
              try {
                //window.open(window.location.href, "_blank");

                if (window.top == window.self) {
                  window.close()
                } else {
                  parent.showElement($(this).attr('data-uri'))
                }
              } catch (e) {
                window.close()
              }
            })
          } else {
            Cookies.set('selURI', '')
            $('#button-run').attr('data-uri', '')
            $('#button-run').css('display', 'none')
            $('#button-run').off('click')
          }
        }
        break

      case 2:
        {
          g_interactionMode = ZOOM_MODE
        }
        break

      case 3:
        {
          g_interactionMode = PAN_MODE
        }
        break
    } // switch (event.which)

    g_interactionMoveInProgress = true

    g_startMouseX = event.clientX
    g_startMouseY = event.clientY
    g_mouseX = event.clientX
    g_mouseY = event.clientY
  } catch (ex) {
    console.error(ex)
  }
})

/*
 * Event handler
 */
$('#canvas-element-id').mousemove(function (event) {
  switch (g_interactionMode) {
    case SELECT_MODE:
      {
        /*
         * Pick
         */
        var x = event.clientX
        var y = event.clientY

        g_viewer.pickObject(x, y)

        /*
         * URI
         */
        if (g_viewer._pickedObject != -1) {
          if (g_instances[g_viewer._pickedObject - 1].uri != undefined) {
            $('#divURL').html(g_instances[g_viewer._pickedObject - 1].uri)
          } else {
            $('#divURL').html(
              g_instances[g_viewer._pickedObject - 1].name +
                ' (' +
                g_instances[g_viewer._pickedObject - 1].group +
                ')'
            )
          }
        } else {
          $('#divURL').html('')
        }
      }
      break

    case ROTATE_MODE:
      {
        rotate(event.clientX, event.clientY)
      }
      break

    case ZOOM_MODE:
      {
        if (g_zoomStartX != -1 && g_zoomStartY != -1) {
          if (
            Math.abs(event.clientX - g_mouseX) !=
            Math.abs(event.clientY - g_mouseY)
          ) {
            var zoomIn = true
            if (
              Math.abs(event.clientX - g_mouseX) >
              Math.abs(event.clientY - g_mouseY)
            ) {
              zoomIn = event.clientX >= g_mouseX ? true : false
            } else {
              zoomIn = event.clientY >= g_mouseY ? false : true
            }

            //console.log(`******* zoom(zoomIn) ${g_zoomStartX} ${g_zoomStartY}`)

            zoom(zoomIn)
          } // if (Math.abs(event.clientX - g_mouseX) != ...
        } // if ((g_zoomStartX != -1) && ...
        else {
          g_zoomStartX = event.clientX
          g_zoomStartY = event.clientY
        }
      }
      break

    case PAN_MODE:
      {
        pan(event.clientX, event.clientY)
      }
      break
  } // switch (g_interactionMode)

  g_mouseX = event.clientX
  g_mouseY = event.clientY

  //console.log(`******* g_mouseXY ${g_mouseX} ${g_mouseY}`)
})

/*
 * Event handler
 */
// http://stackoverflow.com/questions/16788995/mousewheel-event-is-not-triggering-in-firefox-browser
$('#canvas-element-id').on('mousewheel DOMMouseScroll', function (event) {
  if (navigator.userAgent.toLowerCase().indexOf('firefox') > -1) {
    mouseWheelZoom(
      event.originalEvent.detail < 0,
      event.originalEvent.clientX,
      event.originalEvent.clientY,
      Math.abs(event.originalEvent.detail / 3)
    )
  } else {
    mouseWheelZoom(
      event.originalEvent.wheelDelta > 0,
      event.originalEvent.clientX,
      event.originalEvent.clientY,
      Math.abs(event.originalEvent.wheelDelta / 120)
    )
  }
})

/*
 * Event handler
 */

$('#canvas-element-id').mouseup(function (event) {
  var x = event.clientX
  var y = event.clientY

  if (event.ctrlKey) {
    if (event.altKey) {
      /*
       * Reset
       */
      g_viewer.reset()
    } else {
      /*
       * Zoom to
       */
      g_viewer.zoomTo(x, y)
    }
  } else {
    /*
     * Select
     */
    if (g_startMouseX == x && g_startMouseY == y) {
      g_viewer.selectObject(x, y)
    }
  }

  resetInteractionData()
})

$('#canvas-element-id').dblclick(function (event) {
  g_viewer.zoomTo(event.clientX, event.clientY)

  resetInteractionData()
})

/*
 * Event handler
 */
$('#canvas-element-id').mouseleave(function (event) {
  resetInteractionData()
})

/*
 * Event handler
 */
g_onSelectObjectEvent = function (object) {
  try {
    if (typeof getIface === 'function') {
      getIface().sendEvent('', object !== null ? object.uri : null)
    }
  } catch (ex) {
    console.log(ex)
  }
}

g_onHoverObjectEvent = function (object) {}

hideUI = () => {
}

setView = (ev) => {
  const targetData = +ev.target.getAttribute('data')

  g_viewer.setView(targetData)
}

updateSettings = () => {
  const settings = $('#settings-options input')

  settings.each(function () {
    const handlerName = $(this).attr('id')
    const args = $(this).is(':checked')

    settingsHandlersObject[handlerName](args)
  })
}

const settingsHandlersObject = {
  switchCheck3dgrid: (state) => {
    g_viewer._viewOWLGrid = state
  },

  resetSidebarsWidth: (isResetRequested) => {
    if (isResetRequested) {
    }
  },
}
