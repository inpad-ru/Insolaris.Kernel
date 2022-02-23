# Insolaris.Kernel
This repository contains the geometry and model kernel for insolation calculation based on Revit API.

## Math and calculus
### Overview
Mathematical model of calculation method uses right circular conical approximation of sun's vector-function *S(u,v)=p+vg(u)* during 24 hours period. Thus, this method ignores the fact that hodograph of vector-function of sun's direction doesn't actually form an ideal circle and is in fact spiral. But 24 hours span is close enough to form a "circle" and so the approximation won't cause a significant measurement error. Thus, this method's study period is limited to 24 hours.
### Cones
<img src="https://render.githubusercontent.com/render/math?math=\large (l(x-x_1)%2Bm(y-y_1)%2Bn(z-z_1))^2=cos^2(\alpha)(l^{2}%2Bm^{2}%2Bn^{2})((x-x_1)^2%2B(y-y_1)^2%2B(z-z_1)^2)"> **(1)** - general cone equation where

<img src="https://render.githubusercontent.com/render/math?math=\alpha"> - cone's half angle,
<img src="https://render.githubusercontent.com/render/math?math=P(x_1,y_1,z_1)"> - apex,
<img src="https://render.githubusercontent.com/render/math?math=N(l,m,n)"> - axis vector,

**Bounding cone.** Is a mathematical method to limit the sun ray hit angle on a surface point. For instance, if a ray hits a building surface point at an angle >85 degrees, then it most likely won't get inside an abstract room. The idea is to use another cone to bound **the sun cone**. Bounded insolation cone can be found by finding an intersection of two cones with their apex at origin O:

1. Let BC be bounding cone with axis (l,m,n) and angle *a*, let IC be insolation cone with axis (i,j,k) and angle *f*, apex point of both is (0,0,0).
2. Write **(1)** for each cone as:

&nbsp;&nbsp;&nbsp;&nbsp;<img src="https://render.githubusercontent.com/render/math?math=\large (x^2%2By^2%2Bz^2)=\frac{(lx%2Bmy%2Bnz)^2}{cos^2(\alpha)(l^{2}%2Bm^{2}%2Bn^{2})}"> **(2)**

&nbsp;&nbsp;&nbsp;&nbsp;Left part of this equation is 'conical' and right is planar with its normal vector collinear to cone's axis.

3. If two cones at the same apex point intersect, this intersection is generally can be described with equation of two intersecting planes. This equation can be found by equalizing left part of **(2)** of both cones.

&nbsp;&nbsp;&nbsp;&nbsp;<img src="https://render.githubusercontent.com/render/math?math=\large \frac{(lx%2Bmy%2Bnz)^2}{cos^2(\alpha)(l^{2}%2Bm^{2}%2Bn^{2})}=\frac{(ix%2Bjy%2Bkz)^2}{cos^2(f)(i^2%2Bj^2%2Bk^2)}"> **(3)**

Example: https://www.geogebra.org/m/kdk6xwfv. But because we are only looking for positively defined half of a mathematical cone (along the axis vector) the solution is always a regular plane equation.
### Two-cones-intersection plane equation
<img src="https://render.githubusercontent.com/render/math?math=Ax%2BBy%2BCz=0"> - general plane equation.

To find a normal of such a plane PN(A,B,C) is to generalize equation (3). 

Let: 

<img src="https://render.githubusercontent.com/render/math?math=B_1=cos^2(\alpha)(l^{2}%2Bm^{2}%2Bn^{2})">
<img src="https://render.githubusercontent.com/render/math?math=B_2=cos^2(f)(i^{2}%2Bj^{2}%2Bk^{2})">

If axis vectors of BC and IC are unit vector, then both can be simplified as:

<img src="https://render.githubusercontent.com/render/math?math=B_1=cos^2(\alpha)">
<img src="https://render.githubusercontent.com/render/math?math=B_2=cos^2(f)">

Then:

<img src="https://render.githubusercontent.com/render/math?math=\large B_2(lx%2Bmy%2Bnz)^2=B_1(ix%2Bjy%2Bkz)^2">

<img src="https://render.githubusercontent.com/render/math?math=\large \sqrt{B_2}(lx%2Bmy%2Bnz)-\sqrt{B_1}(ix%2Bjy%2Bkz)=0"> 

Modulus was ignored since we're only looking for a positively defined plane along BC axis. Thus:

<img src="https://render.githubusercontent.com/render/math?math=A=\sqrt{B_2}l-\sqrt{B_1}i">
<img src="https://render.githubusercontent.com/render/math?math=B=\sqrt{B_2}m-\sqrt{B_1}j">
<img src="https://render.githubusercontent.com/render/math?math=C=\sqrt{B_2}n-\sqrt{B_1}k">

Or simply:

<img src="https://render.githubusercontent.com/render/math?math=A=cos(f)l-\cos(\alpha)i">
<img src="https://render.githubusercontent.com/render/math?math=B=cos(f)m-\cos(\alpha)j">
<img src="https://render.githubusercontent.com/render/math?math=C=cos(f)n-\cos(\alpha)k">

Such plane with N(A,B,C) is a bounding plane by which we can cut insolation cone.

### Bounding plane
In case the context doesn't defy a bounding cone with angle (0,Pi/2) an insolation cone is still limited by a tangent plane at a given surface point, and it is a more simple subcase of bounding cone method.
