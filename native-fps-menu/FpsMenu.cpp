// Freelancer native Options FPS selector. x86, English 1.1-compatible base only.
// Native UI ABI references: TheStarport/FLUF (AGPL-3.0), BC46/FLSharp (MIT).
// Frame limiter locations: FLHDE/flplusplus (BSD-3-Clause).
// This project is distributed under AGPL-3.0-or-later; see LICENSE.txt.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <cwchar>
#include <cstdint>

namespace {
static_assert(sizeof(void*)==4,"This plugin must be built for x86.");
struct Vec { float x,y,z; };
using InitFn = bool (__thiscall*)(void*,DWORD,DWORD);
using UpdateFn = void (__thiscall*)(void*);
using CreateButtonFn = void* (__thiscall*)(void*,const char*,const char*,const char*,const char*,const char*,const Vec&,const char*,const Vec&,bool,bool,bool,bool,unsigned,unsigned,unsigned);
using CreateTextFn = void* (__thiscall*)(void*,const char*,const char*,const Vec&,const char*,float,float,float,unsigned,int,unsigned,bool);
using PerformFn = int (__thiscall*)(void*,int,uintptr_t,uintptr_t);
InitFn originalInit = nullptr;
UpdateFn originalUpdate = nullptr;
uintptr_t buttonVtable[47]; // RTTI locator plus 46 native virtual methods.
wchar_t iniPath[MAX_PATH];
wchar_t logPath[MAX_PATH];
uintptr_t common = 0;
float* fpsConfig = nullptr;
int selectedFps = 120;
const int choices[] = {60,120,144};

void Log(const char* msg) {
    FILE* f = nullptr;
    if (_wfopen_s(&f, logPath, L"a") == 0) { fprintf(f,"%s\n",msg); fclose(f); }
}

bool Write(uintptr_t addr,const void* value,size_t count) {
    DWORD oldProtect;
    if (!VirtualProtect(reinterpret_cast<void*>(addr),count,PAGE_EXECUTE_READWRITE,&oldProtect)) return false;
    memcpy(reinterpret_cast<void*>(addr),value,count);
    DWORD ignored;
    VirtualProtect(reinterpret_cast<void*>(addr),count,oldProtect,&ignored);
    FlushInstructionCache(GetCurrentProcess(),reinterpret_cast<void*>(addr),count);
    return true;
}

int Perform(void* control,int message,uintptr_t p1=0,uintptr_t p2=0) {
    auto vt=*reinterpret_cast<uintptr_t**>(control);
    return reinterpret_cast<PerformFn>(vt[0xA8/4])(control,message,p1,p2);
}

void* FindChild(void* parent,const char* name) {
    auto child=*reinterpret_cast<unsigned char**>(static_cast<unsigned char*>(parent)+0x0C);
    for (unsigned n=0; child && n<512; ++n) {
        if (strcmp(reinterpret_cast<char*>(child+0x18),name)==0) return child;
        child=*reinterpret_cast<unsigned char**>(child+0x08);
    }
    return nullptr;
}

bool ValidLimiter() {
    if (!common || !fpsConfig) return false;
    MEMORY_BASIC_INFORMATION info{};
    if (!VirtualQuery(fpsConfig,&info,sizeof(info)) || info.State!=MEM_COMMIT ||
        info.AllocationBase!=GetModuleHandleW(L"flplusplus.dll")) return false;
    return *reinterpret_cast<float*>(0x610A0C)==*fpsConfig &&
           *reinterpret_cast<float*>(common+0x1A74C)==*fpsConfig &&
           *fpsConfig>=30.0f && *fpsConfig<=1000.0f;
}

bool SaveAndApply(int next) {
    // Secure access to all three locations before persisting or changing anything.
    const uintptr_t addresses[]={0x610A0C,common+0x1A74C,reinterpret_cast<uintptr_t>(fpsConfig)};
    DWORD protections[3]{};
    int acquired=0;
    for (;acquired<3;++acquired) {
        if (!VirtualProtect(reinterpret_cast<void*>(addresses[acquired]),4,PAGE_EXECUTE_READWRITE,&protections[acquired])) break;
    }
    bool success=false;
    if (acquired==3) {
        wchar_t value[16]; swprintf_s(value,L"%d",next);
        success=WritePrivateProfileStringW(L"flplusplus",L"max_fps",value,iniPath)!=FALSE;
        if (success) {
            const float fps=static_cast<float>(next);
            for (auto address:addresses) memcpy(reinterpret_cast<void*>(address),&fps,4);
            FlushInstructionCache(GetCurrentProcess(),reinterpret_cast<void*>(common+0x1A74C),4);
        }
    }
    for (int i=acquired-1;i>=0;--i) {
        DWORD ignored; VirtualProtect(reinterpret_cast<void*>(addresses[i]),4,protections[i],&ignored);
    }
    return success;
}

void RefreshLabel(void* parent,const wchar_t* error=nullptr) {
    auto text=FindChild(parent,"ModernFPSLabel");
    if (!text) { Log("FPS label was not found in the native child list."); return; }
    wchar_t label[160];
    if (error) swprintf_s(label,L"Frame limit: %s",error);
    else swprintf_s(label,L"FRAME LIMIT: %d FPS%s",selectedFps,selectedFps==144?L" (EXP.)":L"");
    Perform(text,0x1D,reinterpret_cast<uintptr_t>(label));
}

void __fastcall OnFpsClick(void* button,void*) {
    auto parent=*reinterpret_cast<void**>(static_cast<unsigned char*>(button)+4);
    const char* name=reinterpret_cast<char*>(static_cast<unsigned char*>(button)+0x18);
    int index=1;
    for (int i=0;i<3;++i) if (choices[i]==selectedFps) index=i;
    index=(index+(strcmp(name,"ModernFPSPrevious")==0?2:1))%3;
    int next=choices[index];
    if (!ValidLimiter()) { RefreshLabel(parent,L"limiter unavailable"); Log("Limiter validation failed on click."); return; }
    if (!SaveAndApply(next)) {
        RefreshLabel(parent,L"could not save setting"); Log("Could not save max_fps."); return;
    }
    // All changes happen in a native UI callback on the game thread.
    // The existing limiter keeps control of timing; no second limiter is added.
    selectedFps=next;
    RefreshLabel(parent);
    char event[80]; sprintf_s(event,"FPS selected and saved: %d",next); Log(event);
}

bool __fastcall InitOptions(void* parent,void*,DWORD a,DWORD b) {
    bool result=originalInit(parent,a,b);
    if (!result || FindChild(parent,"ModernFPSLabel")) return result;
    const auto createText=reinterpret_cast<CreateTextFn>(0x5A0E30);
    const auto createButton=reinterpret_cast<CreateButtonFn>(0x5A0C90);
    // Native UI coordinates; final positioning is verified against both Options layouts.
    const Vec labelPos{0.0f,0.100f,0.0f};
    void* text=createText(parent,"ModernFPSLabel","SubHeader",labelPos,nullptr,0.33f,0.03f,1.0f,0,0,0,false);
    if (!text) { Log("Text control creation failed."); return result; }
    RefreshLabel(parent);
    *(static_cast<unsigned char*>(text)+0x6C)|=3;
    for (int i=0;i<2;++i) {
        const Vec pos{ i==0 ? 1.46f : 1.66f,0.400f,-3.0f };
        const Vec mouse{0.08f,0.06f,0.0f};
        void* button=createButton(parent,i==0?"ModernFPSPrevious":"ModernFPSNext",
            i==0?"interface\\NEURONET\\INVENTORY\\inv_buyarrow.3db":"interface\\NEURONET\\INVENTORY\\inv_sellarrow.3db",
            nullptr,"ui_select_item",nullptr,pos,nullptr,mouse,false,false,false,false,0,0,0);
        if (button) {
            if (!buttonVtable[0]) {
                memcpy(buttonVtable,*reinterpret_cast<uintptr_t**>(button)-1,sizeof(buttonVtable));
                buttonVtable[1+0xB4/4]=reinterpret_cast<uintptr_t>(&OnFpsClick);
            }
            *reinterpret_cast<uintptr_t**>(button)=buttonVtable+1;
            *(static_cast<unsigned char*>(button)+0x6C)|=3;
        }
    }
    Log("Native FPS controls created.");
    return result;
}

void __fastcall UpdateOptions(void* parent,void*) {
    originalUpdate(parent);
    auto title=FindChild(parent,"Screen Title");
    const wchar_t* caption=title?reinterpret_cast<const wchar_t*>(Perform(title,0x1F)):nullptr;
    bool show=caption && _wcsicmp(caption,L"General Options")==0;
    // Match the game's own label transforms, including JFLP's widescreen offsets.
    auto label=FindChild(parent,"ModernFPSLabel");
    auto gammaLabel=FindChild(parent,"opt_text 2");
    if (label && gammaLabel) {
        auto dst=static_cast<unsigned char*>(label);
        auto src=static_cast<unsigned char*>(gammaLabel);
        memcpy(dst+0x60,src+0x60,sizeof(Vec));
        auto pos=*reinterpret_cast<Vec*>(src+0xA8);
        pos.y-=0.035f;
        memcpy(dst+0xA8,&pos,sizeof(pos));
        memcpy(dst+0x348,src+0x348,4);
        // Arrow meshes have an internal depth of -1. Put them at depth -4
        // to scale them down, and track the native page's position in both
        // main-menu and in-game Options layouts.
        const auto origin=*reinterpret_cast<Vec*>(src+0x60);
        for (int i=0;i<2;++i) {
            auto arrow=FindChild(parent,i==0?"ModernFPSPrevious":"ModernFPSNext");
            if (arrow) {
                const Vec arrowPos{4.0f*(pos.x+origin.x+(i==0?0.473f:0.523f)),
                                   4.0f*(pos.y+origin.y-0.015f),-3.0f};
                memcpy(static_cast<unsigned char*>(arrow)+0xA8,&arrowPos,sizeof(arrowPos));
            }
        }
    }
    const char* names[]={"ModernFPSLabel","ModernFPSPrevious","ModernFPSNext"};
    for (const char* name:names) {
        auto child=FindChild(parent,name);
        if (child) {
            auto flags=static_cast<unsigned char*>(child)+0x6C;
            *flags=static_cast<unsigned char>(show?(*flags|3):(*flags&~2));
        }
    }
}

void Init(HMODULE module) {
    GetModuleFileNameW(module,iniPath,MAX_PATH);
    auto slash=wcsrchr(iniPath,L'\\'); if (!slash) return;
    wcscpy_s(slash+1,MAX_PATH-(slash+1-iniPath),L"FpsMenu.log"); wcscpy_s(logPath,iniPath);
    wcscpy_s(slash+1,MAX_PATH-(slash+1-iniPath),L"flplusplus.ini");
    if (GetModuleHandleW(nullptr)!=reinterpret_cast<HMODULE>(0x400000) || !GetModuleHandleW(L"FLSharp.dll")) {
        Log("Unsupported load order or executable base; FPS menu disabled."); return;
    }
    // Guard the known native creation functions before installing the vtable hook.
    const unsigned char buttonStart[]={0x8B,0x44,0x24,0x04,0x56,0x6A,0x00,0x51,0x68,0x10,0x75,0x5D,0x00};
    const unsigned char textStart[]={0x8B,0x44,0x24,0x04,0x56,0x6A,0x00,0x51,0x68,0xFC,0x73,0x5D,0x00};
    if (memcmp(reinterpret_cast<void*>(0x5A0C90),buttonStart,sizeof(buttonStart)) ||
        memcmp(reinterpret_cast<void*>(0x5A0E30),textStart,sizeof(textStart))) {
        Log("Native UI function signatures differ; FPS menu disabled."); return;
    }
    common=reinterpret_cast<uintptr_t>(GetModuleHandleW(L"common.dll"));
    if (!common || !GetModuleHandleW(L"flplusplus.dll")) { Log("Missing limiter dependency."); return; }
    fpsConfig=*reinterpret_cast<float**>(common+0x1A892);
    if (!ValidLimiter()) { Log("Unsupported limiter state; FPS menu disabled."); return; }
    selectedFps=static_cast<int>(*fpsConfig);
    originalInit=*reinterpret_cast<InitFn*>(0x5D4A80);
    MEMORY_BASIC_INFORMATION initInfo{};
    if (!VirtualQuery(reinterpret_cast<void*>(originalInit),&initInfo,sizeof(initInfo)) ||
        initInfo.AllocationBase!=GetModuleHandleW(L"FLSharp.dll")) {
        Log("Expected FLSharp resolution hook missing; FPS menu disabled."); return;
    }
    const uintptr_t hook=reinterpret_cast<uintptr_t>(&InitOptions);
    originalUpdate=*reinterpret_cast<UpdateFn*>(0x5D4A88);
    if (reinterpret_cast<uintptr_t>(originalUpdate)!=0x4AB6E0) {
        Log("Unexpected Options update hook; FPS menu disabled."); return;
    }
    const uintptr_t update=reinterpret_cast<uintptr_t>(&UpdateOptions);
    if (Write(0x5D4A80,&hook,4)) {
        if (Write(0x5D4A88,&update,4)) {
            char status[80]; sprintf_s(status,"Native FPS menu hook installed; initial cap %d FPS.",selectedFps); Log(status);
        } else {
            Write(0x5D4A80,&originalInit,4);
            Log("Could not install update hook; initialization hook restored.");
        }
    }
}
}

BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID) {
    if (reason==DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(module); Init(module); }
    return TRUE;
}
