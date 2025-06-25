const RULES_CONTAINER = '#ruleDetails'
const RULE_SET_ID_PREFIX = 'ruleset'
const RULESET_CONTAINER_CLASS = '.single-rule'
const RULE_SET_CLASS = 'ruleset-name'
const RULESET_ENABLE_CLASS = '.ruleset-checkbox'
const RULE_CONTAINER_CLASS = '.rule-container'
const RULE_ENABLE_CLASS = '.rule-checkbox'
const RULE_NAME_CLASS = '.rule-name'
const RULE_ISSUE_CLASS = '.rule-indicator'
const CHECKBOX_PARTIAL_ENABLE_CLASS = 'semi-enabled'
const RULE_ISSUE_PREFIX = 'issue-'
const RULE_DETAILS_SELECTOR = '#rule-details'
const rulesDescriptionMap = new Map()
const rulesValueMap = new Map()
const rulesEnableMap = new Map()
let templateR, ruleTemplate
let reportContent

printRuleSets = () => {
  if (embeddedMode()) {
    return;
  }
  
  const container = $(`${RULES_CONTAINER} .rules-container`)
  container.empty()

  initializeTemplatesR()

  const ruleSets = Module.GetRuleSets()
  for (let i = 0; i < ruleSets.size(); i++) {
    const ruleSetContent = getRuleSetContent(ruleSets.get(i), i)

    container.append($(ruleSetContent))

    setRulesAttributes(ruleSets.get(i))
  }

  addEventListeners(container)

  addSplitter()
}

const setRulesAttributes = (handle) => {
  setEnabledRules(handle)

  setRuleName(handle)

  setRuleStatus(handle)
}

const setEnabledRules = (handle) => {
  $(`#${RULE_SET_ID_PREFIX}${handle}`)
    .find(RULE_ENABLE_CLASS)
    .each(function () {
      const ruleHandle = +this.value
      const isEnabled = Module.GetRuleEnabled(ruleHandle)
      $(this).prop('checked', isEnabled)

      rulesEnableMap.set(ruleHandle, isEnabled)
    })
}

const setRuleName = (handle) => {
  $(`#${RULE_SET_ID_PREFIX}${handle}`)
    .find(RULE_NAME_CLASS)
    .each(function () {
      const ruleHandle = +$(this).attr('data')
      $(this).html(getRuleName(ruleHandle))
    })
}

const setRuleStatus = (handle) => {
  $(`#${RULE_SET_ID_PREFIX}${handle}`)
    .find(RULE_ISSUE_CLASS)
    .each(function () {
      for (let i = 0; i < 4; i++) {
        $(this).removeClass(RULE_ISSUE_PREFIX + i)
      }
      const ruleHandle = +$(this).attr('value')
      $(this).addClass(getRuleStatus(ruleHandle))
    })
}

const getRuleSetContent = (handle, index) => {
  const rules = Module.GetRules(handle)
  let rulesContent = ''

  for (let j = 0; j < rules.size(); j++) {
    rulesContent += getRuleContent(rules.get(j))
  }

  return templateR
    .replace(/__RuleOrder__/g, index)
    .replace(/__RuleSetData__/g, handle)
    .replace(/__RuleSetName__/g, getRuleSetName(handle))
    .replace(/__RuleTemplate__/g, rulesContent)
}

const getRuleSetName = (ruleSet) => {
  return Module.GetRuleSetName(ruleSet)
}

const getRuleContent = (handle) => {
  return ruleTemplate.replace(/__RuleData__/g, handle)
}

const getRuleName = (rule) => {
  return Module.GetRuleName(rule)
}

const getRuleStatus = (handle) => {
  return RULE_ISSUE_PREFIX + Module.GetRuleState(handle)
}

const addEventListeners = (container) => {
  addRulesMouseOverListener(container)

  addRuleClickListener(container)

  addCheckBoxesClickListener(container)
}

const addRulesMouseOverListener = (target) => {
  const ruleSetItem = $(target).find(`.${RULE_SET_CLASS}`)
  ruleSetItem.mouseover((e) => {
    e.stopPropagation()
    showRuleDescription(e)
  })

  const ruleItem = $(target).find(RULE_NAME_CLASS)
  ruleItem.mouseover((e) => {
    e.stopPropagation()
    showRuleDescription(e)
  })
}

const showRuleDescription = (event) => {
  const handle = +event.currentTarget.attributes.data.value
  let description = ''

  if (rulesDescriptionMap.has(handle)) {
    description = rulesDescriptionMap.get(handle)
  } else {
    const itemClass = event.currentTarget.attributes.class.value
    let content = ''

    if (itemClass === RULE_SET_CLASS) {
      content = Module.GetRuleSetDescription(handle)
    } else {
      content = Module.GetRuleDescription(handle)
    }

    description = clearDataExtraSymbols(content)

    rulesDescriptionMap.set(handle, description)
  }

  if (description) {
    $(`[data=${handle}]`).attr('title', description)
  }
}

const addRuleClickListener = (target) => {
  const ruleItem = $(target).find(RULE_CONTAINER_CLASS)

  ruleItem.click((e) => {
    e.stopPropagation()
    displayRuleDetails(e)
  })
}

const displayRuleDetails = (event) => {
  const handle = +event.currentTarget.id
  let details = ''

  if (rulesValueMap.has(handle)) {
    details = rulesValueMap.get(handle)
  } else {
    details = Module.GetRuleValue(handle)

    rulesValueMap.set(handle, details)
  }

  highlightSelectedItem(`#${handle}`)

  addRuleDetailData(details)
}

const highlightSelectedItem = (option) => {
  clearRulesArea()

  $(option).addClass(SELECTED_CLASS)
}

const clearRulesArea = () => {
  const container = $(`${RULES_CONTAINER} .rules-container`)
  container.find(`.${SELECTED_CLASS}`).removeClass(SELECTED_CLASS)

  $(RULE_DETAILS_SELECTOR).empty()
}

const addCheckBoxesClickListener = (target) => {
  const ruleSetItem = $(target).find(RULESET_ENABLE_CLASS)
  ruleSetItem.click((e) => {
    e.stopPropagation()
    handleRulesetCheckboxClicked(e)
  })

  const ruleItem = $(target).find(RULE_ENABLE_CLASS)
  ruleItem.change((e) => {
    e.stopPropagation()
    handleRuleCheckboxClicked(e)
  })
  ruleItem.click((e) => {
    e.stopPropagation()
  })
}

const handleRulesetCheckboxClicked = (event) => {
  const checkbox = event.currentTarget
  const isEnabled = $(checkbox).is(':checked')
  const ruleset = $(checkbox).closest(RULESET_CONTAINER_CLASS)

  $(checkbox).removeClass(CHECKBOX_PARTIAL_ENABLE_CLASS)

  updateChildrenVisibility(ruleset, isEnabled)
}

const updateChildrenVisibility = (option, isEnabled) => {
  $(option).find(RULE_ENABLE_CLASS).prop('checked', isEnabled)

  $(option)
    .find(RULE_CONTAINER_CLASS)
    .each(function () {
      const handle = +$(this).attr('id')
      Module.SetRuleEnabled(handle, isEnabled)

      rulesEnableMap.set(handle, isEnabled)
    })
}

const handleRuleCheckboxClicked = (event) => {
  const checkbox = event.currentTarget
  const handle = +event.currentTarget.value

  const isEnabled = $(checkbox).is(':checked')

  Module.SetRuleEnabled(handle, isEnabled)

  rulesEnableMap.set(handle, isEnabled)

  updateParentVisibility(checkbox, isEnabled)
}

const updateParentVisibility = (option, isRuleEnabled) => {
  const checkedPseudoClass = isRuleEnabled ? ':checked' : ':not(:checked)'
  const parent = $(option).closest(RULE_CONTAINER_CLASS)
  const allSiblings = parent.siblings()

  const enabledSiblings = allSiblings.find(
    `${RULE_ENABLE_CLASS}${checkedPseudoClass}`
  )

  const ruleset = $(parent).closest(RULESET_CONTAINER_CLASS)
  const rulesetCheckbox = ruleset.find(RULESET_ENABLE_CLASS).first()

  if (allSiblings.length === enabledSiblings.length) {
    rulesetCheckbox.removeClass(CHECKBOX_PARTIAL_ENABLE_CLASS)
    $(rulesetCheckbox).prop('checked', isRuleEnabled)
  } else {
    rulesetCheckbox.addClass(CHECKBOX_PARTIAL_ENABLE_CLASS)
    $(rulesetCheckbox).prop('checked', true)
  }
}

const initializeTemplatesR = () => {
  templateR = $('#ruleset-template').html()
  ruleTemplate = $('#rule-template').html()
}

const checkRules = () => {
  rulesDescriptionMap.clear()
  rulesValueMap.clear()
  rulesEnableMap.clear()

  Module.CheckRules()

  const ruleSets = Module.GetRuleSets()

  for (let i = 0; i < ruleSets.size(); i++) {
    setRulesAttributes(ruleSets.get(i))
  }
}

const addSplitter = () => {
  Split(['#split-0', '#split-1'], {
    sizes: [50, 50],
    direction: 'vertical',
  })
}

const getReport = () => {
  reportContent = Module.GetRulesReport()

  const regEx = /<body>((.|\n|\r)*)<\/body/g
  const content = regEx.exec(reportContent)[1]

  $('#report-content').html(content)
}

const addRuleDetailData = (data) => {
  //   data = '{"issue1": [{"instanceOne":1}, {"instanceTwo":2}], "issue2": [{"instanceFour":4},{"instanceOne":1},{"instanceSix":6}]}'
  if (data) {
    const issuesList = JSON.parse(data)
    const $issuesRoot = $('<ul class="issues-list">')

    for (const issue in issuesList) {
      if (Object.hasOwnProperty.call(issuesList, issue)) {
        const $issue = $('<li>')
        const issueInstances = issuesList[issue]

        $issue.text(issue)
        $instancesRoot = $('<ul class="issue-instance">')

        for (const instance of issueInstances) {
          const instancesKeys = Object.keys(instance)
          for (const instanceName of instancesKeys) {
            const $instance = $('<li>')
            const $link = $('<a>')

            $link.text(instanceName)
            $link.attr('handle', instance[instanceName])
            $link.click(() => {
              g_viewer._selectedObjects.length = 0
              g_viewer._selectedObjects.push(instance[instanceName])

              displayInTree()
            })

            $instance.append($link)
            $instancesRoot.append($instance)
          }
        }

        $issue.append($instancesRoot)
        $issuesRoot.append($issue)
      }
    }

    $(RULE_DETAILS_SELECTOR).html($issuesRoot)
  } else {
    $(RULE_DETAILS_SELECTOR).html('No issues found')
  }
}

const saveReport = () => {
  const htmlContent = [reportContent.replaceAll('\xEF\xBB\xBF', '')]
  const bl = new Blob(htmlContent, { type: 'text/html', charset: 'UTF-8' })
  const a = document.createElement('a')

  a.href = URL.createObjectURL(bl)
  a.hidden = true
  a.download = createFileName()

  document.body.appendChild(a)
  a.click()
}

const createFileName = () => {
  const openedFileName = $('#leftside .file-name')
    .text()
    .replace(/\.[^/.]+$/, '')

  let date = new Date()
  let day = date.getDate()
  let month = date.getMonth() + 1
  let year = date.getFullYear()
  let hour = date.getHours()
  let minute = date.getMinutes()

  const timestamp = `${year}${month}${day}${hour}${minute}`

  return `report_${openedFileName}_${timestamp}.html`
}
