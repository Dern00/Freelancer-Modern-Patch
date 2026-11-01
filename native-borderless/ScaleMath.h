// SPDX-License-Identifier: MIT
#pragma once
#include <cstdint>
struct FitRect { int x,y,width,height; };
inline FitRect FitAspect(int rw,int rh,int mw,int mh) {
    if (rw<=0 || rh<=0 || mw<=0 || mh<=0) return {0,0,0,0};
    int w=mw,h=mh;
    if (int64_t(mw)*rh>int64_t(mh)*rw) w=int(int64_t(mh)*rw/rh);
    else h=int(int64_t(mw)*rh/rw);
    return {(mw-w)/2,(mh-h)/2,w,h};
}
inline long MapCoordinate(long value,int target,int source) {
    // Floor, including negative coordinates: a point just outside the game
    // must stay outside rather than rounding onto its first clickable pixel.
    int64_t n=int64_t(value)*target;
    return long(n>=0?n/source:-((-n+source-1)/source));
}
