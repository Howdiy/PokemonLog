using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Test : MonoBehaviour
{
    // 구조체 선언 => 접근지정자 struct 구조체명 { 접근지정자 변수선언; || 함수선언; }
    // 클래스 선언 => 접근지정자 class  클래스명 { 접근지정자 변수선언; || 함수선언; }
    struct A // 일반 변수와 동일 ->  값의 복사
    { 
        public int num;
    }
    class B // 포인터와 동일     ->  주소를 참조
    { 
        public int num;
    }


    class A1
    {
        // 구조체 B1의 값을 참조하는 변수 선언
        public B1 b1;
        public int aNum;
    }
    struct B1
    {
        public int bNum;
    }

    // Start is called before the first frame update
    void Start()
    {
        //문제 1
        A c1 = new A();
            c1.num = 10;
        A d1 = c1;
            d1.num = 20;
                Debug.Log(c1.num); // 10
                Debug.Log(d1.num); // 20
        
        B c2 = new B();
            c2.num = 10;
        B d2 = c2;
            d2.num = 20;
                Debug.Log(c2.num); // 20
                Debug.Log(d2.num); // 20

        //문제 2
        Vector3 a = new Vector3(1, 2, 3); 
        
        Vector3 b = a;
            b.x = 10;
            b.y = 20;
            b.z = 30;
                Debug.Log(a); // a => 1, 2, 3
                Debug.Log(b); // b => 10, 20, 30
        
        //문제 3   
        A1 a1 = new A1();       // 변수 a1를 클래스에 선언
        a1.b1 = new B1();       // b1 값를 참조하는 al를 구조체에 할당 -> 임시변수 a1.b1 생성
            a1.aNum = 10;       // 클래스의 aNum에 값 선언
            a1.b1.bNum = 20;    // 임시변수 a1.b1의 bNum 값 선언 -> 임시변수의 a1객체속 bNum의 값 선언 || bNum = 20
        B1 b1 = a1.b1;          // 구조체속 b1의 값을 a1.b1로 초기화
            b1.bNum = 30;       // 임시변수 a1객체속 bNum의 값 초기화 || bNum = 20

            
            a1.b1 = new B1();       // a1.b1를 새로 할당 -> bNum의 값은 0
                b1.bNum = 40;       // bNum값 선언 -> a1.b1에 대한 선언이 아니기에 영향X
        Debug.Log(a1.b1.bNum);  // 0 -> a1.b1초기화후 값을 선언하지 않았기에 기본값 0으로 출력

            A1 a2 = a1;             // a2는 a1를 참조 -> a2.b1 = a1.b1
                a2.b1.bNum = 50;    // 임시변수 a2.b1의 bNum 값 선언
        Debug.Log(a2.b1.bNum);  // 50 -> 클래스에 a2 = a1 선언후 a2.b1의 값을 선언하여 50으로 출력

            A1 a3 = a1;
                a3.b1 = new B1();   // 임시변수 a3.b1를 구조체에 할당
                a3.b1.bNum = 70;    // a3객체속 bNum 값 선언
        Debug.Log(a3.b1.bNum);  // 70 -> 임시변수 a3.b1에 대해 새 할당을 한후 a3객체속 bNum 값을 70으로 초기화
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
