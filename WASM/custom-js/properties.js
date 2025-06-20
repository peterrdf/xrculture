const PROPERTIES_CONTAINER = '#propertyDetails'
const PROPERTY_SET_CLASS = 'propertyset-name'
const PROPERTY_CLASS = 'property-name'
const propsDescriptionMap = new Map()
let template, propertyTemplate

printPropertySets = (handle) => {
  const container = $(`${PROPERTIES_CONTAINER} .rules-container`)
  container.empty()

  initializeTemplates()

  const propertySets = Module.GetPropertySets(handle)
  for (let i = 0; i < propertySets.size(); i++) {
    const propertySetContent = getPropertySetContent(propertySets, i)

    container.append($(propertySetContent))

    addPropertiesMouseOverListener(container)
  }
}

const getPropertySetContent = (propertySets, index) => {
  const handle = propertySets.get(index)
  const properties = Module.GetProperties(handle)
  let propsContent = ''

  for (let j = 0; j < properties.size(); j++) {
    propsContent += getPropertyContent(properties.get(j))
  }

  return template
    .replace(/__PropertyOrder__/g, index)
    .replace(/__PropertySetData__/g, handle)
    .replace(/__PropertySetName__/g, getPropertySetName(handle))
    .replace(/__PropertyTemplate__/g, propsContent)
}

const getPropertySetName = (propertySet) => {
  const name = Module.GetPropertySetName(propertySet)
    .replace('IfcPropertySet', '')
    .trim()

  return name.substring(1, name.length - 1)
}

const getPropertyContent = (handle) => {
  let propertyValue = Module.GetPropertyValue(handle)
  if (propertyValue.indexOf('http') === 0) {
    propertyValue = '<a href="' + propertyValue + '" target="_blank">' + propertyValue + '</a>'
  }

  return propertyTemplate
    .replace(/__PropertyName__/g, getPropertyName(handle))
    .replace(/__PropertyData__/g, handle)
    .replace(/__PropertyDescription__/g, propertyValue)
}

const getPropertyName = (property) => {
  const name = Module.GetPropertyName(property)
    .replace('IfcPropertySingleValue', '')
    .trim()
  return name.substring(1, name.length - 1)
}

const addPropertiesMouseOverListener = (target) => {
  const propSetItem = $(target).find(`.${PROPERTY_SET_CLASS}`)
  propSetItem.mouseover((e) => {
    e.stopPropagation()
    showPropertyDescription(e)
  })

  const propertyItem = $(target).find(`.${PROPERTY_CLASS}`)
  propertyItem.mouseover((e) => {
    e.stopPropagation()
    showPropertyDescription(e)
  })
}

const showPropertyDescription = (event) => {
  const handle = +event.currentTarget.attributes.data.value
  let description = ''

  if (propsDescriptionMap.has(handle)) {
    description = propsDescriptionMap.get(handle)
  } else {
    const itemClass = event.currentTarget.attributes.class.value    
    let content = ''

    if (itemClass === PROPERTY_SET_CLASS) {
      content = Module.GetPropertySetDescription(handle)
    } else {
      content = Module.GetPropertyDescription(handle)
    }

    description = clearDataExtraSymbols(content)

    propsDescriptionMap.set(handle, description)
  }

  if (description) {
    $(`[data=${handle}]`).attr('title', description)
  }
}

const initializeTemplates = () => {
  template = $('#propertyset-template').html()
  propertyTemplate = $('#property-template').html()
}
