'use strict'

const { ModelsRepositoryClient } = require('@azure/iot-modelsrepository')
const hub = require('./app.iothub.js')

const getModel = async (connectionString, id, deviceId) => {
  const url = new URL(id)
  let model
  if (url.pathname === 'azure:common:SelfDescribing;1') {
    console.log('Device is Self Reporting. Querying device for the model . . ')
    const cmdResponse = await hub.invokeDeviceMethod(connectionString, deviceId, 'GetTargetModel')
    model = cmdResponse.payload
  } else {
    const client = new ModelsRepositoryClient({repositoryLocation:'https://raw.githubusercontent.com/iotmodels/iot-plugandplay-models/rido/pnp'})
    const result = await client.getModels(url.protocol + url.pathname)
    if (result) {
      model = result[id]
    }
  }
  return model
}
module.exports = { getModel }
