#ifndef _MAHF_CORE_H_
#define _MAHF_CORE_H_

#include <ntddk.h>

// Device Interface GUID
// {8F9D7A5B-3C2E-4B1F-9A6D-E4C5B7A8D9F0}
DEFINE_GUID(GUID_DEVINTERFACE_MAHF_CPU, 
    0x8f9d7a5b, 0x3c2e, 0x4b1f, 0x9a, 0x6d, 0xe4, 0xc5, 0xb7, 0xa8, 0xd9, 0xf0);

// IOCTL Definitions
#define FILE_DEVICE_MAHF_CPU 0x00008880

#define CTL_CODE_MAHF( Function, Method, Access ) (                 \
    ((FILE_DEVICE_MAHF_CPU) << 16) | ((Access) << 14) |             \
    ((Function) << 2) | (Method)                                    \
)

#define IOCTL_MAHF_GET_CPU_INFO \
    CTL_CODE_MAHF(0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_MAHF_GET_PERFORMANCE_DATA \
    CTL_CODE_MAHF(0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_MAHF_SET_PERFORMANCE_STATE \
    CTL_CODE_MAHF(0x802, METHOD_BUFFERED, FILE_WRITE_DATA)

#define IOCTL_MAHF_RESET_DRIVER \
    CTL_CODE_MAHF(0x803, METHOD_BUFFERED, FILE_WRITE_DATA)

// Performance States
#define PERFORMANCE_STATE_POWER_SAVE    0
#define PERFORMANCE_STATE_BALANCED      1
#define PERFORMANCE_STATE_PERFORMANCE   2
#define PERFORMANCE_STATE_EXTREME       3

// CPU Architectures
#define CPU_ARCH_UNKNOWN    0
#define CPU_ARCH_INTEL      1
#define CPU_ARCH_AMD        2
#define CPU_ARCH_ARM        3

#endif // _MAHF_CORE_H_