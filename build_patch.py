from pathlib import Path
import argparse, hashlib, json, re, shutil

parser=argparse.ArgumentParser(description='Build the patch from the staged downloads; see sources.json for provenance.')
parser.add_argument('--workspace',type=Path,default=Path(__file__).resolve().parent)
parser.add_argument('--source',type=Path,default=Path('C:/Games/Freelancer'))
args=parser.parse_args()
ROOT = args.workspace.resolve()
SOURCE = args.source.resolve()
D = ROOT / 'downloads'
P = ROOT / 'patch' / 'payload'
P.mkdir(parents=True, exist_ok=True)
def copy(src, rel):
    dest = P / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists(): dest.chmod(0o666)
    shutil.copy2(src, dest)
def edit(rel, replacements, expected_sha1=None):
    p = P / rel
    b = bytearray(p.read_bytes())
    if expected_sha1 and hashlib.sha1(b).hexdigest() != expected_sha1:
        raise ValueError(f'Unexpected base: {rel}')
    for offset, before, after in replacements:
        before, after = bytes.fromhex(before), bytes.fromhex(after)
        assert len(before) == len(after)
        if b[offset:offset+len(before)] != before:
            raise ValueError(f'Unexpected bytes: {rel} at {offset:x}')
        b[offset:offset+len(before)] = after
    p.write_bytes(b)

# Extracted from the Microsoft-signed official update, without running its installer.
for folder in ['exe', 'dlls']:
    for p in (D/'official11'/folder).rglob('*'):
        if p.is_file(): copy(p, str(p.relative_to(D/'official11')))
copy(D/'Freelancer-base.exe', 'EXE/Freelancer.exe')
edit('EXE/Freelancer.exe', [(0x1B2D72,'68 80','EB 0F'), (0x2D7B8,'6A 01','EB DD'), (0x158A10,'BC','C0')], '659be06fbbffd0b6cef48824471b94447ae5f65b')
for f in ['jflp.dll','flplusplus.dll','flplusplus_license.txt']:
    copy(D/f, 'EXE/'+f)
copy(D/'HudShift.dll','EXE/HudShift.dll')
for f in ['DATA/cameras.ini','DATA/INTERFACE/HudShift.ini']:
    copy(D/'jflp-extracted'/f,f)
for p in (D/'FLSharp').rglob('*'):
    if p.is_file(): copy(p, 'EXE/'+p.name)
copy(ROOT/'native-fps-menu/build/FpsMenu.dll','EXE/FpsMenu.dll')
copy(ROOT/'native-fps-menu/LICENSE.txt','credits/FpsMenu_LICENSE.txt')
menu_source=ROOT/'patch/source/native-fps-menu'
menu_source.mkdir(parents=True,exist_ok=True)
for filename in ['FpsMenu.cpp','build.ps1','README.txt','LICENSE.txt']:
    shutil.copy2(ROOT/'native-fps-menu'/filename,menu_source/filename)
copy(ROOT/'native-borderless/build/BorderlessScale.dll','EXE/BorderlessScale.dll')
copy(ROOT/'native-borderless/LICENSE.txt','credits/BorderlessScale_LICENSE.txt')
scale_source=ROOT/'patch/source/native-borderless'
scale_source.mkdir(parents=True,exist_ok=True)
for filename in ['BorderlessScale.cpp','ScaleMath.h','ScaleChecks.cpp','build.ps1','README.txt','LICENSE.txt']:
    shutil.copy2(ROOT/'native-borderless'/filename,scale_source/filename)
ini=(P/'EXE/FLSharp.ini').read_text()
enabled={'better_resolutions','post_game_deadlock_fix','quit_message_fix','save_crash_fix','alchemy_crash_fix','freelancer_dll_crash_fix','dealer_menu_open_fix','dealer_crash_fix','base_info_spacing_fix','cursor_fix','mouse_warp_fix','mouse_pos_fix','camera_switch_fix','camera_motion_fix'}
enabled.update({'waypoint_fixes','waypoint_name_fixes','trade_lane_lights_fix',
    'flight_controls_fix','blank_faction_fix','shield_capacity_fix','pilot_names_fix',
    'maneuver_frame_fix','equip_stat_flicker_fix','sub_target_fix','formation_list_fix'})
ini=re.sub(r'^(\w+)\s*=\s*(true|false)',lambda m:f'{m[1]} = {str(m[1] in enabled).lower()}',ini,flags=re.M)
(P/'EXE/FLSharp.ini').write_text(ini)
ini=(D/'flplusplus.ini').read_text()
values={'lod_scale':'1','pbubble_scale':'1','character_detail_scale':'1','save_in_directory':'true','screenshots_in_directory':'true','ship_preview_scrolling_speed':'0','always_regenerate_restart_file':'false','failed_to_init_saves_dir_ids':'0','vsync':'false','max_fps':'120'}
for key,value in values.items(): ini=re.sub(rf'^{key}\s*=.*$',f'{key} = {value}',ini,flags=re.M)
ini=re.sub(r'\[direct_ips\][\s\S]*','[direct_ips]\n',ini)
(P/'EXE/flplusplus.ini').write_text(ini)
ini=(SOURCE/'EXE/dacom.ini').read_text()
ini=ini.replace('.\\flmaterials.dll', '.\\flmaterials.dll\nHudShift.dll\njflp.dll nostats no4way no8way\nflplusplus.dll\nFLSharp.dll\nFpsMenu.dll\nBorderlessScale.dll')
(P/'EXE/dacom.ini').write_text(ini)
# No forced resolution: FLSharp chooses the main monitor; JFLP and HUD Shift adapt.
for f in ['jflpdll.txt','HudShift_readme.txt','BinaryEdits.patch']:
    copy(D/f,'credits/'+f)
manifest=[]
for p in P.rglob('*'):
    if p.is_file():
        rel=p.relative_to(P).as_posix()
        original=SOURCE/rel
        manifest.append({'path':rel,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'original_sha256':hashlib.sha256(original.read_bytes()).hexdigest() if original.exists() else None})
(ROOT/'patch/manifest.json').write_text(json.dumps(manifest,indent=2))
print(f'Prepared {len(manifest)} files; original bytes verified.')
