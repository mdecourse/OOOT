function [f]=g02(x)
%function:
%(1)input:the input variable ,x is one point.it can be a column vector or a row
%vector.
%*****note that It can not evaluate the function at multiple points at
%once,but you can call the fuction multiple times.
%(2)output:f is a scalar,not a vector.
%
%reference:
%note that you can get the formulation of g02 from some
%aritcles,such as
%(1)TP Runarsson, X Yao 'Stochastic Ranking for Constrained Evolutionary Optimization'
% IEEE TRANSACTIONS ON EVOLUTIONARY COMPUTATION, 2000 
%
%solution:
%n=20;lb=zeros(n,1);ub=10*ones(n,1);
%The global minimum is ��

%Copyright:
% programmers:oiltowater.
% It comply with the GPL2.0
% Copyright 2006  oiltowater 



%for get the number of evaluation of function
global functionAcount;
functionAcount=functionAcount+1;


%change the row vector to a column vector.
if size(x,1)==1,
    x=x';
end




n=size(x,1);
numerator=sum(power(cos(x),4))-2*prod(cos(x).*cos(x));

suma=0;
for i=1:n
    suma=suma+i*x(i,1)*x(i,1);
end
denominator=sqrt(suma);

f=numerator/denominator;

f=-f;