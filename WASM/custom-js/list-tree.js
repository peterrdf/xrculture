$.fn.exists = function () {
  return this.length !== 0
}

const STATE_NORMAL = 'normal'
const STATE_3D_SELECT = '3dSelect'
const STATE_VISIBILITY_SELECT = 'visibilitySelect'

const ITEM_CONTAINER_CLASS = 'item-container'
const ITEM_CHILDREN_CONTAINER_TAG = 'ul'
const EXPAND_CLASS_NAME = 'expand'
const STR_EXPAND_CLASS = `.${EXPAND_CLASS_NAME}`
const COLLAPSE_CLASS_NAME = 'collapse'
const VISIBLE_CLASS_NAME = 'visibility'
const STR_VISIBLE_CLASS = `.${VISIBLE_CLASS_NAME}`
const DATA_CLASS_NAME = 'data'
const SELECTED_CLASS = 'selected'
const FULL_VISIBLE = 'full'
const PARTIAL_VISIBLE = 'partial'
const EMPTY_VISIBLE = 'empty'
const VISIBLE_STATES_CLASSES = [FULL_VISIBLE, PARTIAL_VISIBLE, EMPTY_VISIBLE]
const descriptionMap = new Map()
const visibilityMap = new Map()
const itemVisibleIndexesMap = new Map()
const ownVisibleIndexMap = new Map()
let activeTree = 0
let selectedGInstanceIndex = -1
let isSetToVisible = undefined
let state = STATE_NORMAL

const isAutoNavigate = () => selectedGInstanceIndex !== -1

const log = (content) => {
  $('#console').append(`<div>${content}</div>`)
  console.log(content)
}

function resetFields() {
  activeTree = 0
  selectedGInstanceIndex = -1
  isSetToVisible = undefined
  state = STATE_NORMAL

  $('#select-container').children().empty()
  $('#tree-labels').empty()

  descriptionMap.clear()
  visibilityMap.clear()
  itemVisibleIndexesMap.clear()
  ownVisibleIndexMap.clear()
  propsDescriptionMap.clear()

  clearPropertiesArea()

  clearRulesArea()
}

function clearPropertiesArea() {
  const container = $(`${PROPERTIES_CONTAINER} .rules-container`)
  container.empty()
}

function updateFileNameField(name) {
  $('.file-name').text(name)

  $('#rhside01 a').show()
}

function loadAllIFCTrees() {
  const treeTypeCnt = Module.GetTreeTypeCnt()

  for (let treeType = 0; treeType < treeTypeCnt; treeType++) {
    loadTree(treeType)
  }
}

function loadTree(treeType) {
  const treeHandleHead = Module.GetTreeHeadHandle(treeType)

  setTreeTab(treeType, treeHandleHead)

  addTreeRootToHtml(treeType)

  loadTreeChildren(treeHandleHead, getTreeId(treeType))
}

function setTreeTab(treeType, handle) {
  const treeName = Module.GetTreeItemName(handle).replace(/tree/gi, '').trim()

  $('#tree-labels').append(
    $('<a>', {
      text: treeName,
      id: `label${treeType}`,
    }).click(() => {
      showTree(treeType)
    })
  )

  showTree(0)
}

function showTree(treeType) {
  const treeTypeCnt = Module.GetTreeTypeCnt()
  for (let i = 0; i < treeTypeCnt; i++) {
    const treeID = getTreeId(i)
    const labelID = `#label${i}`

    activeTree = treeType

    if (i === treeType) {
      $(treeID).show()
      $(labelID).addClass('active-label')
    } else {
      $(treeID).hide()
      $(labelID).removeClass('active-label')
    }
  }

  updateTreeItemsVisibility()

  updateTreeSelectedItems()
}

function addTreeRootToHtml(treeType) {
  const $root = $('<ul>', {
    id: treeId(treeType),
  }).addClass('form-control')

  $('#tree-container').append($root)
}

function loadTreeChildren(handle, parent) {
  const childrenCnt = Module.GetTreeChildCnt(handle)

  if (childrenCnt) {
    if (hasChildrenContainer(parent)) {
      const parentContainer = $(`<${ITEM_CHILDREN_CONTAINER_TAG}>`)
      $(parent).append(parentContainer)
      parent = parentContainer
    }

    for (let childIndex = 0; childIndex < childrenCnt; childIndex++) {
      const handleChild = Module.GetTreeChildHandle(handle, childIndex)

      addTreeChildToHtml(handleChild, parent)
    }
  }
}

function hasChildrenContainer(parent) {
  return !$(parent).children(`${ITEM_CHILDREN_CONTAINER_TAG}`).length
}

function addTreeChildToHtml(handle, parent) {
  const option = getHtmlOption(handle, parent)

  check3DtoItemState(option)
}

function check3DtoItemState(option) {
  const handle = getHandle(option)

  if (isAutoNavigate()) {
    if (containsGeometry(handle)) {
      if (getTreeItemOwnVisibleItem(handle) === selectedGInstanceIndex) {
        updateTargetItem(option)
      } else if (shouldExpandItem(option)) {
        fireExpandClick(option)
      } else {
        loadTreeChildren(handle, option)
      }
    }
  }
}

function updateTargetItem(option) {
  if (state === STATE_3D_SELECT) {
    setSelectedItem(option)
  }
  if (state === STATE_VISIBILITY_SELECT) {
    setVisibleItem(option)
  }
}

function setSelectedItem(option) {
  const handle = getHandle(option)
  printPropertySets(handle)

  const $target = $(option).find(`.${ITEM_CONTAINER_CLASS}`).first()
  $target.addClass(SELECTED_CLASS)
  $target.get(0).scrollIntoView(false)

  selectedGInstanceIndex = -1

  setState(STATE_NORMAL)
}

function setVisibleItem(option) {
  setVisibleStateClass(option, isSetToVisible)
}

function shouldExpandItem(option) {
  const expandContainer = getExpand(option)

  return expandContainer.attr('class') === EXPAND_CLASS_NAME
}

function getHtmlOption(handle, parent) {
  const existingOption = getOption(handle)
  let option

  if (existingOption.exists()) {
    option = existingOption
  } else {
    option = createOption(handle, parent)

    setChildText(option)

    setItemHtml(option)

    addListeners(option)
  }

  return option
}

function createOption(handle, parent) {
  const option = $('<li>', {
    value: handle,
  })

  $(parent).append(option)

  return option
}

function addListeners(option) {
  addClickListener(option)

  addMouseOverListener(option)

  addRightClickListener(option)
}

function setChildText(option) {
  const handle = getHandle(option)
  const name = Module.GetTreeItemName(handle)

  $(option).append(name)
}

function setItemHtml(option) {
  const container = $('<div>').addClass(ITEM_CONTAINER_CLASS)

  addExpandHtml(container, option)

  addVisibilityControlHtml(container, option)

  addDataHtml(container, option)

  $(option).empty()
  $(option).append(container)
}

function addExpandHtml(container, option) {
  const hasChildren = !!Module.GetTreeChildCnt(getHandle(option))
  const expand = $('<span>')

  if (hasChildren) {
    $(expand).text('+')
    $(expand).addClass(EXPAND_CLASS_NAME)
  } else {
    $(expand).addClass('no-expand')
  }

  $(container).append(expand)
}

function addVisibilityControlHtml(container, option) {
  const visible = $('<span>')
  $(visible).addClass(VISIBLE_CLASS_NAME)
  $(visible).text('@')
  $(container).append(visible)

  let visibilityClass = 'none'
  const handle = getHandle(option)
  const handleVisibleItems = getTreeItemVisibleItemsIndexes(handle)
  const handleVisibleItemsCount = handleVisibleItems.length

  if (handleVisibleItemsCount) {
    const parentOption = $(option).parent().closest('li')

    if (parentOption.exists()) {
      const parentHandle = getHandle(parentOption)
      visibilityClass = visibilityMap.get(parentHandle)

      if ([FULL_VISIBLE, EMPTY_VISIBLE].indexOf(visibilityClass) === -1) {
        const visibleItemIndex = getTreeItemOwnVisibleItem(handle)

        if (visibleItemIndex !== -1) {
          if (handleVisibleItemsCount > 1) {
            visibilityClass = getVisibilityWhenMultiChildren(handleVisibleItems)
          } else if (g_instances[visibleItemIndex].visible) {
            visibilityClass = FULL_VISIBLE
          } else {
            visibilityClass = EMPTY_VISIBLE
          }
        } else {
          visibilityClass = getVisibilityWhenMultiChildren(handleVisibleItems)
        }
      }
    } else {
      visibilityClass = FULL_VISIBLE
    }
  }

  $(visible).addClass(visibilityClass)

  visibilityMap.set(handle, visibilityClass)
}

function getVisibilityWhenMultiChildren(visibleIndexes) {
  const hasSomeChildNotVisible = visibleIndexes.some(
    (index) => !g_instances[index].visible
  )

  return hasSomeChildNotVisible ? PARTIAL_VISIBLE : FULL_VISIBLE
}

function addDataHtml(container, option) {
  const optionText = $(option).text()

  const data = $('<span>')
  $(data).addClass(DATA_CLASS_NAME)
  $(data).text(optionText)
  $(container).append(data)
}

function addMouseOverListener(option) {
  $(option).mouseover((e) => {
    e.stopPropagation()
    handleMouseOver(e)
  })
}

function handleMouseOver(event) {
  const handle = +event.currentTarget.value
  let description = ''

  if (descriptionMap.has(handle)) {
    description = descriptionMap.get(handle)
  } else {
    description = clearDataExtraSymbols(Module.GetTreeItemDescription(handle))

    descriptionMap.set(handle, description)
  }

  if (description) {
    getOption(handle).attr('title', description)
  }
}

function clearDataExtraSymbols(description) {
  // special symbols detected ASCII 10, 13;
  return description.replaceAll(String.fromCharCode(13), '')
}

function addClickListener(option) {
  $(option)
    .children(`.${ITEM_CONTAINER_CLASS}`)
    .first()
    .click((event) => {
      event.stopPropagation()
      const option = $(event.currentTarget).parent()
      const targetClasses = $(event.target).attr('class')

      if (targetClasses) {
        targetClasses.split(' ').forEach((targetClass) => {
          switch (targetClass) {
            case EXPAND_CLASS_NAME:
              handleExpandClick(option)
              return
            case VISIBLE_CLASS_NAME:
              set3DVisibility(option)
              return
            case DATA_CLASS_NAME:
              highlightIn3D(option)
              return
          }
        })
      }
    })
}

function addRightClickListener(option) {
  $(option).contextmenu(function (event) {
    event.stopPropagation()

    const targetClass = $(event.target).attr('class')

    hideMenu()
    if (targetClass === DATA_CLASS_NAME) {
      event.preventDefault()

      const menu = $('#contextMenu')

      menu.show()
      menu.css('left', event.pageX + 'px')
      menu.css('top', event.pageY + 'px')

      $('#view-item').attr('data', getHandle(option))
    }
  })
}

function hideMenu() {
  $('#contextMenu').hide()
}

function handleViewItemClick(event) {
  const target = event.target
  const handle = +$(target).attr('data')

  highlightIn3D(getOption(handle), true)
}

function fireExpandClick(option) {
  const expandContainer = getExpand(option)

  expandContainer.trigger('click')
}

function handleExpandClick(option) {
  const handle = getHandle(option)

  if ($(option).children().length > 1) {
    $(option)
      .find(`> ${ITEM_CHILDREN_CONTAINER_TAG}`)
      .toggle(0, onCollapseCallback)

    if (state === STATE_3D_SELECT) {
      loadTreeChildren(handle, option)
    }
  } else {
    loadTreeChildren(handle, option)
  }

  updateExpandStateClass(option)
}

function onCollapseCallback() {
  // highlight element if it has a 'selected' child
  const clickedItem = $(this)
  const isCollapsed = clickedItem.is(':hidden')
  if (isCollapsed) {
    if (clickedItem.parent().find(`.${SELECTED_CLASS}`).length) {
      cleanSelectedItems(true)

      clickedItem
        .parent()
        .find(`.${ITEM_CONTAINER_CLASS}`)
        .first()
        .addClass(SELECTED_CLASS)
    }
  }
}

function updateExpandStateClass(option) {
  const clickedExpandItem = getExpand(option)
  clickedExpandItem.toggleClass(COLLAPSE_CLASS_NAME)
}

function set3DVisibility(option) {
    const handle = getHandle(option)
  const treeItemVisibleItems = getTreeItemVisibleItemsIndexes(handle)

  if (treeItemVisibleItems.length) {
    setState(STATE_VISIBILITY_SELECT)

    const isVisible = !g_instances[treeItemVisibleItems[0]].visible

    treeItemVisibleItems.forEach(
      (index) => (g_instances[index].visible = isVisible)
    )

    setVisibleStateClass(option, isVisible)

    PENDING_DRAW_SCENE = true
  } else {
    console.error(`${handle} has no geometry`)
  }
}

function setVisibleStateClass(option, isVisible) {
  updateItemAndChildrenVisibility(option, isVisible)
  updateParentsVisibility(option)

  if (!isVisible) {
    deselectItemIn3DView(option)
    removeOptionSelectedClass(option)
  }
}

function updateItemAndChildrenVisibility(option, isVisible) {
  let visibilityClass

  removeVisibilityClasses(option, true)

  $(option)
    .find(STR_VISIBLE_CLASS)
    .each(function () {
      const handle = +$(this).closest('li').attr('value')

      if (getTreeItemVisibleItemsIndexes(handle).length) {
        visibilityClass = isVisible ? FULL_VISIBLE : EMPTY_VISIBLE
      } else {
        visibilityClass = 'none'
      }

      $(this).addClass(visibilityClass)
      visibilityMap.set(handle, visibilityClass)
    })
}

function updateParentsVisibility(option) {
  const siblings = $(option).siblings()
  const siblingsCount = getSiblingsWithGeometry(option)

  const optionVisibilityClasses = $(option)
    .find(STR_VISIBLE_CLASS)
    .first()
    .attr('class')

  if (optionVisibilityClasses) {
    const stateClass = optionVisibilityClasses
      .replace(VISIBLE_CLASS_NAME, '')
      .trim()
    const visibleSiblingsCount = siblings.find(
      `${STR_VISIBLE_CLASS}.${stateClass}`
    ).length

    if (siblingsCount === visibleSiblingsCount) {
      setParentVisibility(option, stateClass)
    } else {
      setParentsToPartialVisibility(option)
    }
  }
}

function setParentVisibility(option, visibilityClass) {
  const parentOption = $(option).parent().closest('li')
  const visibleParentContainer = getVisibilityContainer(parentOption)
  const parentHandle = getHandle(parentOption)

  removeVisibilityClasses(parentOption)

  visibilityClass = calcVisibilityClass(visibilityClass, parentHandle)

  $(visibleParentContainer).addClass(visibilityClass)
  visibilityMap.set(parentHandle, visibilityClass)

  if (
    parentOption.parent(ITEM_CHILDREN_CONTAINER_TAG).attr('id') !==
    treeId(activeTree)
  ) {
    updateParentsVisibility(parentOption)
  }
}

//** calculate visibility class if own 3D visible items are displayed/hidden
function calcVisibilityClass(defaultVisibility, handle) {
  if (defaultVisibility !== PARTIAL_VISIBLE) {
    const own3DIndex = getTreeItemOwnVisibleItem(handle)

    if (own3DIndex !== -1) {
      const isOwn3DVisible = g_instances[own3DIndex].visible

      if (
        (defaultVisibility === EMPTY_VISIBLE && isOwn3DVisible) ||
        (defaultVisibility === FULL_VISIBLE && !isOwn3DVisible)
      ) {
        defaultVisibility = PARTIAL_VISIBLE
      }
    }
  }
  return defaultVisibility
}

function setParentsToPartialVisibility(option) {
  const parents = $(option).parents('li')

  parents.each(function () {
    updateItemVisibility(this, PARTIAL_VISIBLE)
  })
}

function updateItemVisibility(option, visibilityClass) {
  removeVisibilityClasses(option)

  getVisibilityContainer(option).addClass(visibilityClass)

  visibilityMap.set(getHandle(option), visibilityClass)
}

function removeVisibilityClasses(option, isDeepSearch = false) {
  VISIBLE_STATES_CLASSES.forEach((stateClass) => {
    if (isDeepSearch) {
      $(option).find(STR_VISIBLE_CLASS).removeClass(stateClass)
    } else {
      getVisibilityContainer(option).removeClass(stateClass)
    }
  })
}

function deselectItemIn3DView(option) {
  const handle = getHandle(option)
  const treeItemVisibleItems = getTreeItemVisibleItemsIndexes(handle)

  if (treeItemVisibleItems.length && g_viewer._selectedObjects.length) {
    const selectedObjects = []

    g_viewer._selectedObjects.forEach((selectedIndex) => {
      if (!treeItemVisibleItems.includes(selectedIndex - 1)) {
        selectedObjects.push(selectedIndex)
      }
    })

    g_viewer._selectedObjects = selectedObjects
  }
}

function removeOptionSelectedClass(option) {
  const $target = $(option).find(`.${ITEM_CONTAINER_CLASS}`).first()
  $target.removeClass(SELECTED_CLASS)
}

function getSiblingsWithGeometry(option) {
  // find siblings of option that are not visibility=none
  const siblings = $(option).siblings()
  const siblingsCount = siblings.find(`${STR_VISIBLE_CLASS}:not(.none)`).length

  return siblingsCount
}

function displayInTree() {
  setState(STATE_3D_SELECT)
  cleanSelectedItems()

  reduceMultipleSelected3dObjectsToOne()

  if (g_viewer._selectedObjects.length === 1) {
    const rootHandle = Module.GetTreeHeadHandle(activeTree)
    selectedGInstanceIndex = g_viewer._selectedObjects[0] - 1

    if (containsGeometry(rootHandle)) {
      loadTreeChildren(rootHandle, getTreeId(activeTree))
    }
  }
}

function reduceMultipleSelected3dObjectsToOne() {
  if (g_viewer._selectedObjects.length > 1) {
    const firstElement = g_viewer._selectedObjects[0]
    g_viewer._selectedObjects.length = 0
    g_viewer._selectedObjects.push(firstElement)
  }
}

function containsGeometry(handle) {
  return Module.TreeItemContainsVisibleItem(handle, selectedGInstanceIndex)
}

function cleanSelectedItems(inActiveTree = false) {
  const treeId = inActiveTree ? getTreeId(activeTree) : '#tree-container'

  $(treeId).find(`.${SELECTED_CLASS}`).removeClass(SELECTED_CLASS)

  clearPropertiesArea()
}

function updateTreeSelectedItems() {
  displayInTree()
}

function setState(newState) {
  state = newState
}

function printPropertySets(handle) {
  // empty
}

function printRuleSets(handle) {
  // empty
}

function updateTreeItemsVisibility() {
  if (hasAllInstancesHidden()) {
    setVisibilityToAll(EMPTY_VISIBLE)
  } else if (hasSomeHiddenInstances()) {
    traverseTreeItems(getTreeId(activeTree))
  } else {
    setVisibilityToAll(FULL_VISIBLE)
  }
}

function setVisibilityToAll(vClass) {
  removeVisibilityClasses($(getTreeId()), true)

  $(getTreeId()).find(`${STR_VISIBLE_CLASS}:not(.none)`).addClass(vClass)

  $(getTreeId())
    .find('li')
    .each(function () {
      const handle = getHandle(this)

      visibilityMap.set(handle, vClass)
    })
}

function traverseTreeItems(rootId) {
  const root = $(rootId)
  const containers = root.find(`.${ITEM_CONTAINER_CLASS}`)
  const leafItems = containers.filter(function () {
    const isLeaf =
      $(this).parent().find(`.${ITEM_CONTAINER_CLASS}`).length === 1

    return isLeaf
  })

  leafItems.each(function () {
    const option = $(this).parent()
    const handle = getHandle(option)

    const visibleIndexes = getTreeItemVisibleItemsIndexes(handle)

    if (areAllIndexesVisible(visibleIndexes)) {
      setVisibleStateClass(option, true)
    } else if (areAllIndexesHidden(visibleIndexes)) {
      setVisibleStateClass(option, false)
    } else {
      removeVisibilityClasses(option)
      getVisibilityContainer(this).addClass(PARTIAL_VISIBLE)
      visibilityMap.set(handle, PARTIAL_VISIBLE)
      updateParentsVisibility(option)
    }
  })
}

function areAllIndexesVisible(visibleIndexes) {
  return visibleIndexes.every((index) => {
    return g_instances[index].visible
  })
}

function areAllIndexesHidden(visibleIndexes) {
  return visibleIndexes.every((index) => {
    return !g_instances[index].visible
  })
}

function collapseAllTreeItems() {
  const treeRootSelector = `${getTreeId()} .${ITEM_CONTAINER_CLASS} + ${ITEM_CHILDREN_CONTAINER_TAG}`
  const treeExpandedItemsSelector = `${getTreeId()} .${ITEM_CONTAINER_CLASS} > ${STR_EXPAND_CLASS}`

  $(treeRootSelector).hide()
  $(treeExpandedItemsSelector).removeClass(COLLAPSE_CLASS_NAME)

  updateCollapsedSelectedItems()
}

function updateCollapsedSelectedItems() {
  const $selectedContainer = $(
    `${getTreeId()} .${ITEM_CONTAINER_CLASS}.${SELECTED_CLASS}`
  )
  const selectedOptionParent = $selectedContainer.parent().parent().get(0)

  onCollapseCallback.call(selectedOptionParent)
}

function collapseSelectedTreeItems() {
  const selectedItemsSelector = `${getTreeId()} .${ITEM_CONTAINER_CLASS}.${SELECTED_CLASS}`
  const childrenContentSelector = `${selectedItemsSelector} + ${ITEM_CHILDREN_CONTAINER_TAG}`

  $(selectedItemsSelector)
    .find(STR_EXPAND_CLASS)
    .removeClass(COLLAPSE_CLASS_NAME)
  const children = $(childrenContentSelector).find(STR_EXPAND_CLASS)
  children.removeClass(COLLAPSE_CLASS_NAME)

  $(childrenContentSelector).hide()
  children.parent().find(`+ ${ITEM_CHILDREN_CONTAINER_TAG}`).hide()
}

function hideAllTreeItems() {
  setVisibilityToAll(EMPTY_VISIBLE)

  g_instances.forEach((instance) => (instance.visible = false))
}

function hideSelectedTreeItems() {
  const selectedItemsSelector = `${getTreeId()} .${SELECTED_CLASS}`
  const option = $(selectedItemsSelector).parent()

  presetOptionAsVisible(option)
  set3DVisibility(option)

  g_viewer._selectedObjects = []
  $(selectedItemsSelector).removeClass(SELECTED_CLASS)

  clearPropertiesArea()
}

function presetOptionAsVisible(option) {
  const handle = getHandle(option)
  const treeItemVisibleItems = getTreeItemVisibleItemsIndexes(handle)

  if (treeItemVisibleItems.length) {
    g_instances[treeItemVisibleItems[0]].visible = true
  }
}

function highlightIn3D(option, zoom = false) {
  const handle = getHandle(option)
  const visibleIndexes = getTreeItemVisibleItemsIndexes(handle)

  cleanSelectedItems()

  if (visibleIndexes.length) {
    const $target = $(option).find(`.${ITEM_CONTAINER_CLASS}`).first()
    $target.addClass(SELECTED_CLASS)

    printPropertySets(handle)

    g_viewer.selectObjectByIndexes(visibleIndexes)

    if (zoom) {
      g_viewer.zoomToObject(g_instances[visibleIndexes[0]])
    }
  } else {
    g_viewer._selectedObjects = []
  }
}

function hasSomeHiddenInstances() {
  const visibleInstances = g_instances.filter((inst) => inst.visible)

  return g_instances.length !== visibleInstances.length
}

function hasAllInstancesHidden() {
  const visibleInstances = g_instances.filter((inst) => inst.visible)

  return visibleInstances.length === 0
}

// getters
function getHandle(option) {
  return +$(option).attr('value')
}

function getOption(handle) {
  return $(`[value=${handle}]`)
}

function treeId(type = activeTree) {
  return `tree${type}`
}

function getTreeId(type = activeTree) {
  return `#${treeId(type)}`
}

function getExpand(option) {
  return $(option).find(STR_EXPAND_CLASS).first()
}

function getVisibilityContainer(option) {
  return $(option).find(STR_VISIBLE_CLASS).first()
}

function getTreeItemVisibleItemsIndexes(handle) {
  const visibleIndexes = []

  if (itemVisibleIndexesMap.has(handle)) {
    visibleIndexes.push(...itemVisibleIndexesMap.get(handle))
  } else {
    const treeItemVisibleItems = Module.GetTreeItemVisibleItems(handle)

    for (let i = 0; i < treeItemVisibleItems.size(); i++) {
      visibleIndexes.push(treeItemVisibleItems.get(i))
    }

    itemVisibleIndexesMap.set(handle, visibleIndexes)
  }

  return visibleIndexes
}

function getTreeItemOwnVisibleItem(handle) {
  if (ownVisibleIndexMap.has(handle)) {
    return ownVisibleIndexMap.get(handle)
  }

  const visibleIndex = Module.GetTreeItemOwnVisibleItem(handle)

  ownVisibleIndexMap.set(handle, visibleIndex)

  return visibleIndex
}

function getInvisibleInstances() {
  return g_instances.reduce((acc, instance, index) => {
    if (!instance.visible) {
      acc.push(index)
    }
    return acc
  }, [])
}
