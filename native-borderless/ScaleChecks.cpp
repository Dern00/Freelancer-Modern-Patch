#include "ScaleMath.h"
#include <cassert>
#include <cstdio>
#include <cstdlib>
int main() {
    auto f=FitAspect(1920,1080,2560,1440);
    assert(f.x==0 && f.y==0 && f.width==2560 && f.height==1440);
    f=FitAspect(1600,1200,2560,1440);
    assert(f.x==320 && f.y==0 && f.width==1920 && f.height==1440);
    f=FitAspect(2560,1440,3440,1440);
    assert(f.x==440 && f.y==0 && f.width==2560 && f.height==1440);
    f=FitAspect(3440,1440,2560,1440);
    assert(f.x==0 && f.y>0 && f.width==2560 && f.height==1071);
    for (int x=-1;x<=2560;++x) {
        auto logical=MapCoordinate(x,1920,2560);
        auto physical=MapCoordinate(logical,2560,1920);
        assert(std::abs(physical-x)<=2);
        if (x<0) assert(logical<0);
        if (x>=2560) assert(logical>=1920);
        if (x>=0 && x<2560) assert(logical>=0 && logical<1920);
    }
    assert(MapCoordinate(1280,1920,2560)==960);
    assert(MapCoordinate(960,2560,1920)==1280);
    assert(FitAspect(0,1080,2560,1440).width==0);
    puts("Aspect fit, centered bars, coordinate bounds and round trips passed.");
}
