#define RIGHT  (1 << 0) // 1
#define LEFT   (1 << 1) // 2
#define TOP    (1 << 2) // 4
#define BOTTOM (1 << 3) // 8

#define RIGHT_LEFT            (RIGHT | LEFT)                // 3
#define TOP_RIGHT             (TOP | RIGHT)                 // 5
#define TOP_LEFT              (TOP | LEFT)                  // 6
#define TOP_RIGHT_LEFT        (TOP | RIGHT | LEFT)          // 7
#define BOTTOM_RIGHT          (BOTTOM | RIGHT)              // 9
#define BOTTOM_LEFT           (BOTTOM | LEFT)               // 10
#define BOTTOM_RIGHT_LEFT     (BOTTOM | RIGHT | LEFT)       // 11
#define TOP_BOTTOM            (TOP | BOTTOM)                // 12
#define TOP_RIGHT_BOTTOM      (TOP | RIGHT | BOTTOM)        // 13
#define TOP_LEFT_BOTTOM       (TOP | LEFT | BOTTOM)         // 14
#define TOP_RIGHT_LEFT_BOTTOM (TOP | RIGHT | LEFT | BOTTOM) // 15