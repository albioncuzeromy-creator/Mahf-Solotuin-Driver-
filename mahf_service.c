/*
 * Mahf Firmware CPU Driver - User Mode Service
 * Copyright (c) 2024 Mahf Corporation
 * 
 * Manages driver communication and system integration
 * Version: 3.0.0
 */

#include <windows.h>
#include <stdio.h>
#include <tchar.h>
#include <strsafe.h>

// Service configuration
#define SERVICE_NAME  _T("MahfCPUService")
#define SERVICE_DISPLAY_NAME  _T("Mahf CPU Service")
#define SERVICE_DESCRIPTION  _T("Manages Mahf Firmware CPU Driver")

// Global variables
SERVICE_STATUS g_ServiceStatus = {0};
SERVICE_STATUS_HANDLE g_StatusHandle = NULL;
HANDLE g_ServiceStopEvent = INVALID_HANDLE_VALUE;

// Driver communication handle
HANDLE g_DriverHandle = INVALID_HANDLE_VALUE;

// Function declarations
VOID WINAPI ServiceMain(DWORD argc, LPTSTR *argv);
VOID WINAPI ServiceCtrlHandler(DWORD);
DWORD WINAPI ServiceWorkerThread(LPVOID lpParam);
BOOL InitializeDriverConnection();
VOID CloseDriverConnection();
BOOL SendDriverCommand(DWORD ioControlCode, LPVOID inputBuffer, DWORD inputSize, LPVOID outputBuffer, DWORD outputSize);

// Service entry point
int _tmain(int argc, TCHAR *argv[])
{
    SERVICE_TABLE_ENTRY ServiceTable[] = 
    {
        {SERVICE_NAME, (LPSERVICE_MAIN_FUNCTION) ServiceMain},
        {NULL, NULL}
    };
    
    if (StartServiceCtrlDispatcher(ServiceTable) == FALSE)
    {
        // If running as console app
        if (GetLastError() == ERROR_FAILED_SERVICE_CONTROLLER_CONNECT)
        {
            printf("This program is a service and cannot be run as console application.\n");
            printf("To install service: %s --install\n", argv[0]);
            printf("To uninstall service: %s --uninstall\n", argv[0]);
        }
        return GetLastError();
    }
    
    return 0;
}

// Service main function
VOID WINAPI ServiceMain(DWORD argc, LPTSTR *argv)
{
    DWORD Status = E_FAIL;
    
    UNREFERENCED_PARAMETER(argc);
    UNREFERENCED_PARAMETER(argv);
    
    // Register service control handler
    g_StatusHandle = RegisterServiceCtrlHandler(SERVICE_NAME, ServiceCtrlHandler);
    
    if (g_StatusHandle == NULL)
    {
        return;
    }
    
    // Initialize service status
    ZeroMemory(&g_ServiceStatus, sizeof(g_ServiceStatus));
    g_ServiceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    g_ServiceStatus.dwControlsAccepted = 0;
    g_ServiceStatus.dwCurrentState = SERVICE_START_PENDING;
    g_ServiceStatus.dwWin32ExitCode = 0;
    g_ServiceStatus.dwServiceSpecificExitCode = 0;
    g_ServiceStatus.dwCheckPoint = 0;
    
    // Report initial status
    if (SetServiceStatus(g_StatusHandle, &g_ServiceStatus) == FALSE)
    {
        OutputDebugString(_T("ServiceMain: SetServiceStatus returned error"));
    }
    
    // Create stop event
    g_ServiceStopEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    if (g_ServiceStopEvent == NULL)
    {
        g_ServiceStatus.dwCurrentState = SERVICE_STOPPED;
        g_ServiceStatus.dwWin32ExitCode = GetLastError();
        SetServiceStatus(g_StatusHandle, &g_ServiceStatus);
        return;
    }
    
    // Initialize driver connection
    if (!InitializeDriverConnection())
    {
        g_ServiceStatus.dwCurrentState = SERVICE_STOPPED;
        g_ServiceStatus.dwWin32ExitCode = ERROR_SERVICE_SPECIFIC_ERROR;
        g_ServiceStatus.dwServiceSpecificExitCode = 1;
        SetServiceStatus(g_StatusHandle, &g_ServiceStatus);
        return;
    }
    
    // Report running status
    g_ServiceStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP;
    g_ServiceStatus.dwCurrentState = SERVICE_RUNNING;
    g_ServiceStatus.dwWin32ExitCode = 0;
    g_ServiceStatus.dwCheckPoint = 0;
    
    if (SetServiceStatus(g_StatusHandle, &g_ServiceStatus) == FALSE)
    {
        OutputDebugString(_T("ServiceMain: SetServiceStatus returned error"));
    }
    
    // Start worker thread
    HANDLE hThread = CreateThread(NULL, 0, ServiceWorkerThread, NULL, 0, NULL);
    
    // Wait for stop signal
    WaitForSingleObject(g_ServiceStopEvent, INFINITE);
    
    // Wait for worker thread to finish
    WaitForSingleObject(hThread, 5000);
    
    // Cleanup
    CloseHandle(hThread);
    CloseDriverConnection();
    CloseHandle(g_ServiceStopEvent);
    
    // Report stopped status
    g_ServiceStatus.dwControlsAccepted = 0;
    g_ServiceStatus.dwCurrentState = SERVICE_STOPPED;
    g_ServiceStatus.dwWin32ExitCode = 0;
    g_ServiceStatus.dwCheckPoint = 3;
    
    if (SetServiceStatus(g_StatusHandle, &g_ServiceStatus) == FALSE)
    {
        OutputDebugString(_T("ServiceMain: SetServiceStatus returned error"));
    }
}

// Service control handler
VOID WINAPI ServiceCtrlHandler(DWORD CtrlCode)
{
    switch (CtrlCode)
    {
        case SERVICE_CONTROL_STOP:
            if (g_ServiceStatus.dwCurrentState != SERVICE_RUNNING)
                break;
            
            g_ServiceStatus.dwControlsAccepted = 0;
            g_ServiceStatus.dwCurrentState = SERVICE_STOP_PENDING;
            g_ServiceStatus.dwWin32ExitCode = 0;
            g_ServiceStatus.dwCheckPoint = 4;
            
            if (SetServiceStatus(g_StatusHandle, &g_ServiceStatus) == FALSE)
            {
                OutputDebugString(_T("ServiceCtrlHandler: SetServiceStatus returned error"));
            }
            
            SetEvent(g_ServiceStopEvent);
            break;
            
        default:
            break;
    }
}

// Worker thread function
DWORD WINAPI ServiceWorkerThread(LPVOID lpParam)
{
    UNREFERENCED_PARAMETER(lpParam);
    
    // Main service loop
    while (WaitForSingleObject(g_ServiceStopEvent, 0) != WAIT_OBJECT_0)
    {
        // Service functionality here
        // Monitor system, communicate with driver, etc.
        
        Sleep(1000); // Check every second
    }
    
    return ERROR_SUCCESS;
}

// Initialize driver connection
BOOL InitializeDriverConnection()
{
    // Open driver device
    g_DriverHandle = CreateFile(
        _T("\\\\.\\MahfCPU"),
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    
    if (g_DriverHandle == INVALID_HANDLE_VALUE)
    {
        DWORD error = GetLastError();
        
        // Log error
        TCHAR errorMsg[256];
        StringCchPrintf(errorMsg, 256, 
            _T("Failed to open driver device. Error: %d"), error);
        OutputDebugString(errorMsg);
        
        return FALSE;
    }
    
    OutputDebugString(_T("Driver connection initialized successfully"));
    return TRUE;
}

// Close driver connection
VOID CloseDriverConnection()
{
    if (g_DriverHandle != INVALID_HANDLE_VALUE)
    {
        CloseHandle(g_DriverHandle);
        g_DriverHandle = INVALID_HANDLE_VALUE;
        OutputDebugString(_T("Driver connection closed"));
    }
}

// Send command to driver
BOOL SendDriverCommand(DWORD ioControlCode, LPVOID inputBuffer, DWORD inputSize, 
                       LPVOID outputBuffer, DWORD outputSize)
{
    if (g_DriverHandle == INVALID_HANDLE_VALUE)
        return FALSE;
    
    DWORD bytesReturned = 0;
    
    return DeviceIoControl(
        g_DriverHandle,
        ioControlCode,
        inputBuffer,
        inputSize,
        outputBuffer,
        outputSize,
        &bytesReturned,
        NULL);
}

// Install service
BOOL InstallService()
{
    SC_HANDLE scmHandle = NULL;
    SC_HANDLE serviceHandle = NULL;
    TCHAR servicePath[MAX_PATH];
    BOOL result = FALSE;
    
    // Get current executable path
    if (GetModuleFileName(NULL, servicePath, MAX_PATH) == 0)
    {
        printf("GetModuleFileName failed. Error: %d\n", GetLastError());
        return FALSE;
    }
    
    // Open Service Control Manager
    scmHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
    if (scmHandle == NULL)
    {
        printf("OpenSCManager failed. Error: %d\n", GetLastError());
        return FALSE;
    }
    
    // Create service
    serviceHandle = CreateService(
        scmHandle,
        SERVICE_NAME,
        SERVICE_DISPLAY_NAME,
        SERVICE_ALL_ACCESS,
        SERVICE_WIN32_OWN_PROCESS,
        SERVICE_AUTO_START,
        SERVICE_ERROR_NORMAL,
        servicePath,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL);
    
    if (serviceHandle == NULL)
    {
        DWORD error = GetLastError();
        if (error == ERROR_SERVICE_EXISTS)
        {
            printf("Service already exists.\n");
            result = TRUE;
        }
        else
        {
            printf("CreateService failed. Error: %d\n", error);
        }
    }
    else
    {
        printf("Service installed successfully.\n");
        
        // Set service description
        SERVICE_DESCRIPTION description = {SERVICE_DESCRIPTION};
        ChangeServiceConfig2(serviceHandle, SERVICE_CONFIG_DESCRIPTION, &description);
        
        CloseServiceHandle(serviceHandle);
        result = TRUE;
    }
    
    CloseServiceHandle(scmHandle);
    return result;
}

// Uninstall service
BOOL UninstallService()
{
    SC_HANDLE scmHandle = NULL;
    SC_HANDLE serviceHandle = NULL;
    BOOL result = FALSE;
    
    // Open Service Control Manager
    scmHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
    if (scmHandle == NULL)
    {
        printf("OpenSCManager failed. Error: %d\n", GetLastError());
        return FALSE;
    }
    
    // Open service
    serviceHandle = OpenService(scmHandle, SERVICE_NAME, DELETE);
    if (serviceHandle == NULL)
    {
        printf("OpenService failed. Error: %d\n", GetLastError());
        CloseServiceHandle(scmHandle);
        return FALSE;
    }
    
    // Delete service
    if (DeleteService(serviceHandle))
    {
        printf("Service deleted successfully.\n");
        result = TRUE;
    }
    else
    {
        printf("DeleteService failed. Error: %d\n", GetLastError());
    }
    
    CloseServiceHandle(serviceHandle);
    CloseServiceHandle(scmHandle);
    return result;
}