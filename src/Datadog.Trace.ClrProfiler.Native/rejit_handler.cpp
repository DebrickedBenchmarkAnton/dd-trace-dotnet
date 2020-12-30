#include "rejit_handler.h"

namespace trace {

void RejitHandlerModuleMethod::AddFunctionId(FunctionID functionId) {
  std::lock_guard<std::mutex> guard(functionsIds_lock);
  auto moduleHandler = (RejitHandlerModule*)module;
  auto rejitHandler = (RejitHandler*)moduleHandler->GetHandler();
  functionsIds.insert(functionId);
  rejitHandler->_addFunctionToSet(functionId, this);
}
bool RejitHandlerModuleMethod::ExistFunctionId(FunctionID functionId) {
  std::lock_guard<std::mutex> guard(functionsIds_lock);
  return functionsIds.find(functionId) != functionsIds.end();
}

RejitHandlerModuleMethod* RejitHandlerModule::GetOrAddMethod(mdMethodDef methodDef) {
  std::lock_guard<std::mutex> guard(methods_lock);

  if (methods.count(methodDef) > 0) {
    return methods[methodDef];
  }

  RejitHandlerModuleMethod* methodHandler = new RejitHandlerModuleMethod(methodDef, this);
  methods[methodDef] = methodHandler;
  return methodHandler;
}
bool RejitHandlerModule::TryGetMethod(mdMethodDef methodDef,
                                      RejitHandlerModuleMethod** methodHandler) {
  std::lock_guard<std::mutex> guard(methods_lock);

  if (methods.count(methodDef) > 0) {
    *methodHandler = methods[methodDef];
    return true;
  }
  *methodHandler = nullptr;
  return false;
}

RejitHandlerModuleMethod* RejitHandler::GetModuleMethodFromFunctionId(
    FunctionID functionId) {
  {
    std::lock_guard<std::mutex> guard(methodByFunctionId_lock);
    if (methodByFunctionId.count(functionId) > 0) {
      return methodByFunctionId[functionId];
    }
  }

  ModuleID moduleId;
  mdToken function_token = mdTokenNil;

  HRESULT hr = profilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId,
                                             &function_token);

  if (FAILED(hr)) {
    Warn(
        "RejitHandler::GetModuleMethodFromFunctionId: Call to "
        "ICorProfilerInfo4.GetFunctionInfo() "
        "failed for ",
        functionId);
    methodByFunctionId[functionId] = nullptr;
    return nullptr;
  }

  auto moduleHandler = GetOrAddModule(moduleId);
  auto methodHandler = moduleHandler->GetOrAddMethod(function_token);
  methodHandler->AddFunctionId(functionId);
  return methodHandler;
}

RejitHandlerModule* RejitHandler::GetOrAddModule(ModuleID moduleId) {
  std::lock_guard<std::mutex> guard(modules_lock);

  if (modules.count(moduleId) > 0) {
    return modules[moduleId];
  }

  RejitHandlerModule* moduleHandler = new RejitHandlerModule(moduleId, this);
  modules[moduleId] = moduleHandler;
  return moduleHandler;
}

bool RejitHandler::TryGetModule(ModuleID moduleId,
                              RejitHandlerModule** moduleHandler) {
  std::lock_guard<std::mutex> guard(modules_lock);
  if (modules.count(moduleId) > 0) {
    *moduleHandler = modules[moduleId];
    return true;
  }
  *moduleHandler = nullptr;
  return false;
}

HRESULT RejitHandler::NotifyReJITParameters(
    ModuleID moduleId, mdMethodDef methodId,
    ICorProfilerFunctionControl* pFunctionControl, ModuleMetadata* metadata) {
  auto moduleHandler = GetOrAddModule(moduleId);
  moduleHandler->SetModuleMetadata(metadata);
  auto methodHandler = moduleHandler->GetOrAddMethod(methodId);
  methodHandler->SetFunctionControl(pFunctionControl);
  
  if (methodHandler->GetMethodDef() == mdMethodDefNil) {
    Warn(
        "NotifyReJITCompilationStarted: mdMethodDef is missing for "
        "MethodDef: ", 
        methodId);
    return S_FALSE;
  }

  if (methodHandler->GetFunctionControl() == nullptr) {
    Warn(
        "NotifyReJITCompilationStarted: ICorProfilerFunctionControl is missing "
        "for "
        "MethodDef: ",
        methodId);
    return S_FALSE;
  }

  if (methodHandler->GetFunctionInfo() == nullptr) {
    Warn(
        "NotifyReJITCompilationStarted: FunctionInfo is missing for "
        "MethodDef: ",
        methodId);
    return S_FALSE;
  }

  if (methodHandler->GetMethodReplacement() == nullptr) {
    Warn(
        "NotifyReJITCompilationStarted: MethodReplacement is missing for "
        "MethodDef: ",
        methodId);
    return S_FALSE;
  }

  if (moduleHandler->GetModuleId() == 0) {
    Warn(
        "NotifyReJITCompilationStarted: ModuleID is missing for "
        "MethodDef: ",
        methodId);
    return S_FALSE;
  }

  if (moduleHandler->GetModuleMetadata() == nullptr) {
    Warn(
        "NotifyReJITCompilationStarted: ModuleMetadata is missing for "
        "MethodDef: ",
        methodId);
    return S_FALSE;
  }

  return rewriteCallback(moduleHandler, methodHandler);
}

HRESULT RejitHandler::NotifyReJITCompilationStarted(FunctionID functionId, ReJITID rejitId) {
  return S_OK;
}

void RejitHandler::_addFunctionToSet(FunctionID functionId,
                                     RejitHandlerModuleMethod* method) {
  std::lock_guard<std::mutex> guard(methodByFunctionId_lock);
  methodByFunctionId[functionId] = method;
}

}  // namespace trace