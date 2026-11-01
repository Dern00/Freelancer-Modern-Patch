// SPDX-License-Identifier: MIT
// Borderless presentation scaling for the checked English Freelancer executable.
// FLSharp mouse coordinate references: BC46/FLSharp src/mouse.cpp (MIT).
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <cwchar>
#include <cstdint>
#include "ScaleMath.h"
#pragma comment(lib,"user32.lib")
#pragma comment(lib,"gdi32.lib")

namespace {
static_assert(sizeof(void*)==4,"Build for x86.");
HMODULE self;
HWND game, backdrop;
WNDPROC previousProc;
int renderWidth,renderHeight;
bool sizing=false;
wchar_t logPath[MAX_PATH];

void Log(const char* message) {
    FILE* f=nullptr;
    if (_wfopen_s(&f,logPath,L"a")==0) { fprintf(f,"%s\n",message); fclose(f); }
}
bool BorderlessMode() {
    return *reinterpret_cast<WORD*>(0x42477A)==0 &&
           *reinterpret_cast<WORD*>(0x42490D)==0 &&
           *reinterpret_cast<BYTE*>(0x5B16CC)==0 &&
           *reinterpret_cast<BYTE*>(0x6679F0)==0; // actual window mode, including Alt+Enter
}
bool Active(HWND window) {
    return window && window==game && BorderlessMode() && renderWidth>=320 && renderHeight>=200;
}
LRESULT CALLBACK BackdropProc(HWND window,UINT msg,WPARAM wp,LPARAM lp) {
    if (msg==WM_MOUSEACTIVATE) return MA_NOACTIVATE;
    if (msg==WM_LBUTTONDOWN) { if (game) SetForegroundWindow(game); return 0; }
    if (msg==WM_ERASEBKGND) {
        RECT r; GetClientRect(window,&r);
        FillRect(reinterpret_cast<HDC>(wp),&r,static_cast<HBRUSH>(GetStockObject(BLACK_BRUSH)));
        return 1;
    }
    return DefWindowProcW(window,msg,wp,lp);
}
void UpdateBackdrop() {
    if (!Active(game) || IsIconic(game) || !IsWindowVisible(game) || GetForegroundWindow()!=game) {
        if (backdrop) ShowWindow(backdrop,SW_HIDE);
        return;
    }
    MONITORINFO mi{sizeof(mi)};
    RECT r{};
    if (!GetMonitorInfoW(MonitorFromWindow(game,MONITOR_DEFAULTTONEAREST),&mi) || !GetWindowRect(game,&r)) return;
    if (EqualRect(&r,&mi.rcMonitor)) { if (backdrop) ShowWindow(backdrop,SW_HIDE); return; }
    if (!backdrop) {
        WNDCLASSW wc{}; wc.lpfnWndProc=BackdropProc; wc.hInstance=self;
        wc.lpszClassName=L"FreelancerScaleBackdrop";
        wc.hbrBackground=static_cast<HBRUSH>(GetStockObject(BLACK_BRUSH));
        RegisterClassW(&wc);
        backdrop=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE,wc.lpszClassName,L"",WS_POPUP,
            0,0,0,0,nullptr,nullptr,self,nullptr);
    }
    if (backdrop) {
        SetWindowPos(backdrop,game,mi.rcMonitor.left,mi.rcMonitor.top,
            mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top,
            SWP_NOACTIVATE|SWP_SHOWWINDOW);
    }
}
void FitWindow() {
    if (!Active(game) || sizing || IsIconic(game)) return;
    MONITORINFO mi{sizeof(mi)};
    if (!GetMonitorInfoW(MonitorFromWindow(game,MONITOR_DEFAULTTONEAREST),&mi)) return;
    auto fit=FitAspect(renderWidth,renderHeight,mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top);
    RECT target{mi.rcMonitor.left+fit.x,mi.rcMonitor.top+fit.y,
        mi.rcMonitor.left+fit.x+fit.width,mi.rcMonitor.top+fit.y+fit.height};
    RECT current{}; GetWindowRect(game,&current);
    if (!EqualRect(&current,&target)) {
        sizing=true;
        SetWindowPos(game,nullptr,target.left,target.top,fit.width,fit.height,SWP_NOZORDER|SWP_NOACTIVATE);
        sizing=false;
    }
    UpdateBackdrop();
}
LRESULT CALLBACK WindowProc(HWND window,UINT msg,WPARAM wp,LPARAM lp) {
    if (msg==WM_SIZE && wp!=SIZE_MINIMIZED && Active(window)) lp=MAKELPARAM(renderWidth,renderHeight);
    auto result=CallWindowProcA(previousProc,window,msg,wp,lp);
    if (msg==WM_ACTIVATE || msg==WM_ACTIVATEAPP || msg==WM_SHOWWINDOW || msg==WM_SIZE) UpdateBackdrop();
    if (msg==WM_DISPLAYCHANGE && Active(window)) FitWindow();
    if (msg==WM_NCDESTROY) {
        if (backdrop) DestroyWindow(backdrop);
        backdrop=nullptr; game=nullptr;
    }
    return result;
}
BOOL WINAPI ScaleSetWindowPos(HWND window,HWND after,int x,int y,int cx,int cy,UINT flags) {
    // Restrict resizing to the game's own main-window sizing routine. Other
    // windows, normal windowed mode and exclusive fullscreen retain native behavior.
    if (BorderlessMode() && window==*reinterpret_cast<HWND*>(0x6679F4) &&
        !(flags&SWP_NOSIZE) && cx>=320 && cy>=200) {
        if (!game) {
            game=window;
            previousProc=reinterpret_cast<WNDPROC>(SetWindowLongPtrA(window,GWLP_WNDPROC,reinterpret_cast<LONG_PTR>(WindowProc)));
            if (!previousProc) { game=nullptr; return SetWindowPos(window,after,x,y,cx,cy,flags); }
        }
        renderWidth=cx; renderHeight=cy;
        MONITORINFO mi{sizeof(mi)};
        if (GetMonitorInfoW(MonitorFromWindow(window,MONITOR_DEFAULTTONEAREST),&mi)) {
            auto fit=FitAspect(cx,cy,mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top);
            x=mi.rcMonitor.left+fit.x; y=mi.rcMonitor.top+fit.y; cx=fit.width; cy=fit.height;
            flags&=~SWP_NOMOVE;
            char message[160]; sprintf_s(message,"Render %dx%d -> client %dx%d at %d,%d.",renderWidth,renderHeight,cx,cy,x,y); Log(message);
        }
        sizing=true;
        auto result=SetWindowPos(window,after,x,y,cx,cy,flags);
        sizing=false;
        UpdateBackdrop();
        return result;
    }
    return SetWindowPos(window,after,x,y,cx,cy,flags);
}
BOOL WINAPI ScaleScreenToClient(HWND window,POINT* p) {
    if (!ScreenToClient(window,p)) return FALSE;
    RECT r{};
    if (Active(window) && GetClientRect(window,&r) && r.right>0 && r.bottom>0) {
        p->x=MapCoordinate(p->x,renderWidth,r.right);
        p->y=MapCoordinate(p->y,renderHeight,r.bottom);
    }
    return TRUE;
}
BOOL WINAPI ScaleClientToScreen(HWND window,POINT* p) {
    RECT r{};
    if (Active(window) && GetClientRect(window,&r) && r.right>0 && r.bottom>0) {
        p->x=MapCoordinate(p->x,r.right,renderWidth);
        p->y=MapCoordinate(p->y,r.bottom,renderHeight);
    }
    return ClientToScreen(window,p);
}
BOOL WINAPI ScaleGetClientRect(HWND window,RECT* r) {
    if (!GetClientRect(window,r)) return FALSE;
    if (Active(window)) { r->right=renderWidth; r->bottom=renderHeight; }
    return TRUE;
}
struct Hook { const char* name; uintptr_t replacement; };
struct Slot { uintptr_t* address; uintptr_t before; uintptr_t after; };
bool Collect(HMODULE module,const Hook* hooks,size_t count,Slot* slots,size_t& size) {
    auto base=reinterpret_cast<BYTE*>(module);
    auto dos=reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    auto nt=reinterpret_cast<IMAGE_NT_HEADERS*>(base+dos->e_lfanew);
    auto rva=nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;
    if (!rva) return false;
    for (auto desc=reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base+rva);desc->Name;++desc) {
        if (_stricmp(reinterpret_cast<char*>(base+desc->Name),"USER32.dll")) continue;
        if (!desc->OriginalFirstThunk) return false;
        auto names=reinterpret_cast<IMAGE_THUNK_DATA*>(base+desc->OriginalFirstThunk);
        auto values=reinterpret_cast<IMAGE_THUNK_DATA*>(base+desc->FirstThunk);
        for (;names->u1.AddressOfData;++names,++values) {
            if (IMAGE_SNAP_BY_ORDINAL(names->u1.Ordinal)) continue;
            auto name=reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base+names->u1.AddressOfData)->Name;
            for (size_t i=0;i<count;++i) if (!strcmp(reinterpret_cast<char*>(name),hooks[i].name)) {
                auto original=reinterpret_cast<uintptr_t>(GetProcAddress(GetModuleHandleW(L"user32.dll"),hooks[i].name));
                if (values->u1.Function!=original || size>=16) return false;
                slots[size++]={reinterpret_cast<uintptr_t*>(&values->u1.Function),original,hooks[i].replacement};
            }
        }
    }
    return true;
}
void Init(HMODULE module) {
    self=module;
    GetModuleFileNameW(module,logPath,MAX_PATH);
    auto slash=wcsrchr(logPath,L'\\'); if (!slash) return;
    wcscpy_s(slash+1,MAX_PATH-(slash+1-logPath),L"BorderlessScale.log");
    auto exe=GetModuleHandleW(nullptr); auto sharp=GetModuleHandleW(L"FLSharp.dll");
    if (exe!=reinterpret_cast<HMODULE>(0x400000) || !sharp) { Log("Unsupported base/load order; disabled."); return; }
    const BYTE signature[]={0x8A,0x44,0x24,0x08,0x83,0xEC,0x10,0x84,0xC0};
    if (memcmp(reinterpret_cast<void*>(0x424870),signature,sizeof(signature))) { Log("Window routine signature differs; disabled."); return; }
    const Hook hooks[]={
        {"SetWindowPos",reinterpret_cast<uintptr_t>(ScaleSetWindowPos)},
        {"ScreenToClient",reinterpret_cast<uintptr_t>(ScaleScreenToClient)},
        {"ClientToScreen",reinterpret_cast<uintptr_t>(ScaleClientToScreen)},
        {"GetClientRect",reinterpret_cast<uintptr_t>(ScaleGetClientRect)}};
    Slot slots[16]{}; size_t count=0;
    if (!Collect(exe,hooks,4,slots,count) || count!=4 || !Collect(sharp,hooks+1,3,slots,count) || count<6) {
        Log("Unexpected imports; disabled without changes."); return;
    }
    // Secure all pages first. The import slots can share a page, so restore in
    // reverse order after the transaction, including on a protection failure.
    DWORD protections[16]{}; size_t acquired=0;
    for (;acquired<count;++acquired)
        if (!VirtualProtect(slots[acquired].address,sizeof(uintptr_t),PAGE_READWRITE,&protections[acquired])) break;
    if (acquired==count) for (size_t i=0;i<count;++i) *slots[i].address=slots[i].after;
    for (size_t i=acquired;i>0;--i) { DWORD ignored; VirtualProtect(slots[i-1].address,sizeof(uintptr_t),protections[i-1],&ignored); }
    Log(acquired==count?"Borderless scaling imports installed.":"Import protection failed; disabled without changes.");
}
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID) {
    if (reason==DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(module); Init(module); }
    return TRUE;
}
