# Insolaris.Kernel
This repository contains the geometry and model kernel for insolation calculation based on Revit API.

## Math and calculus
### Overview
Mathematical model of calculation method uses right circular conical approximation of sun's vector-function *S(u,v)=p+vg(u)* during 24 hours period. Thus, this method ignores the fact that hodograph of vector-function of sun's direction doesn't actually form an ideal circle and is in fact spiral. But 24 hours span is close enough to form a "circle" and so the approximation won't cause a significant measurement error. Thus, this method's study period is limited to 24 hours.
### Cones
> x^2+y^2+z^2=(lx+my+nz)^2/cos^2(a)(l^2+m^2+n^2) - general cone equation with apex at origin O.

> N(l,m,n) - cone's axis vector, a - cone's half angle.

**Bounding cone.** Is a mathematical method to limit the sun ray hit angle on a surface point. For instance, if a ray hits a building surface point at an angle >85 degrees, then it most likely won't get inside an abstract room. The idea is to use another cone to bound **the sun cone**. Bounding can be found by creating a math model:
> (lx+my+nz)^2/cos^2(a)(l^2+m^2+n^2)=(ix+jy+kz)^2/cos^2(f)(i^2+j^2+k^2) - (2), where N2(i,j,k) is normal at a point and an axis vector of bounding cone, f is a half angle of bounding cone.

if such cones intersect, this intersection (2) is an equation of two intersecting planes. Example: https://www.geogebra.org/m/kdk6xwfv. But because we are only looking for positively defined half of a mathematical cone (along the axis vector) the solution is always a regular plane equation.
### Two-cones-intersection plane equation
> Ax+By+Cz=0 - general plane equation.

To find such a normal of such a plane PN(A,B,C) is to generalize equation (2). Solution:
>Let B1=cos^2(a)(l^2+m^2+n^2), B2=cos^2(f)(i^2+j^2+k^2)

> Then A=lsqrt(B2)-isqrt(B1), B=msqrt(B2)-jsqrt(B1), C=nsqrt(B2)-ksqrt(B1), But since (l,m,n) and (i,j,k) is always normalized B1=cos^2(a), B2=cos^2(f)
