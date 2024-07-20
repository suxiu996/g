using System;
using System.Collections.Generic;
using FK_CLI;

// ウィンドウ設定メソッド
fk_AppWindow WindowSetup()
{
    var win = new fk_AppWindow();
    win.Size = new fk_Dimension(600, 600);
    win.BGColor = new fk_Color(0.6, 0.7, 0.8);
    win.ShowGuide(fk_Guide.GRID_XY);
    win.CameraPos = new fk_Vector(0.0, 0.0, 80.0);
    win.CameraFocus = new fk_Vector(0.0, 0.0, 0.0);
    win.TrackBallMode = true;
    win.Open();
    return win;
}

// 制御点生成メソッド
fk_Model makeCtrlPoint(fk_AppWindow argWin, fk_Sphere argSp)
{
    var plane = new fk_Plane();
    plane.SetPosNormal(new fk_Vector(0.0, 0.0, 0.0), new fk_Vector(0.0, 0.0, 1.0));
    var pos3D = new fk_Vector();
    var pos2D = argWin.MousePosition;
    argWin.GetProjectPosition(pos2D.x, pos2D.y, plane, pos3D);
    var model = new fk_Model();
    model.Shape = argSp;
    model.Material = fk_Material.Yellow;
    model.GlMoveTo(pos3D);
    return model;
}

// Bézier点計算メソッド
fk_Vector CalcBezierPoint(List<fk_Vector> points, double t)
{
    double u = 1 - t;
    double tt = t * t;
    double uu = u * u;
    double uuu = uu * u;
    double ttt = tt * t;

    fk_Vector p = uuu * points[0]; // (1-t)^3 * P0
    p += 3 * uu * t * points[1];   // 3 * (1-t)^2 * t * P1
    p += 3 * u * tt * points[2];   // 3 * (1-t) * t^2 * P2
    p += ttt * points[3];          // t^3 * P3

    return p;
}

// C1連続性を保証する点の計算メソッド
fk_Vector CalcC1ContinuousPoint(fk_Vector p2, fk_Vector p3, fk_Vector p4)
{
    return 2 * p3 - p2;
}

// G1連続性を保証する点の計算メソッド
fk_Vector CalcG1ContinuousPoint(fk_Vector p2, fk_Vector p3, fk_Vector p4)
{
    var direction = p3 - p2;
    var length = (p4 - p3).Dist() * 0.5;
    direction.Normalize();
    return p3 + direction * length;
}

// モーションパス上の点を計算するメソッド
fk_Vector GetBezierPoint(List<fk_Vector> ctrlPoints1, List<fk_Vector> ctrlPoints2, double t)
{
    if (t < 0.5)
    {
        return CalcBezierPoint(ctrlPoints1, t * 2);
    }
    else
    {
        return CalcBezierPoint(ctrlPoints2, (t - 0.5) * 2);
    }
}

// モーションパス上の接線を計算するメソッド
fk_Vector GetBezierTangent(List<fk_Vector> ctrlPoints1, List<fk_Vector> ctrlPoints2, double t)
{
    double delta = 0.01;
    if (t < 0.5)
    {
        var p1 = CalcBezierPoint(ctrlPoints1, t * 2);
        var p2 = CalcBezierPoint(ctrlPoints1, t * 2 + delta);
        return p2 - p1;
    }
    else
    {
        var p1 = CalcBezierPoint(ctrlPoints2, (t - 0.5) * 2);
        var p2 = CalcBezierPoint(ctrlPoints2, (t - 0.5) * 2 + delta);
        return p2 - p1;
    }
}

// Main
var win = WindowSetup();
var mList = new List<fk_Model>();
var ctrlPoints = new List<fk_Vector>();
var sphere = new fk_Sphere(8, 1.0);
bool lineFlg = false;
bool g1Flag = false;

win.Open();

// 円錐モデル設定
var coneModel = new fk_Model();
var cone = new fk_Cone(16, 1.0, 2.0);
coneModel.Shape = cone;
coneModel.Material = fk_Material.Green;
win.Entry(coneModel);

while (win.Update())
{
    if (win.GetMouseStatus(fk_MouseButton.M1, fk_Switch.DOWN, true) && mList.Count < 6)
    {
        var model = makeCtrlPoint(win, sphere);
        win.Entry(model);
        mList.Add(model);
        ctrlPoints.Add(model.Position);
    }

    if (mList.Count == 6 && !lineFlg)
    {
        var bezier1 = ctrlPoints.GetRange(0, 4);
        fk_Vector q;

        if (g1Flag)
        {
            q = CalcG1ContinuousPoint(ctrlPoints[2], ctrlPoints[3], ctrlPoints[4]);
        }
        else
        {
            q = CalcC1ContinuousPoint(ctrlPoints[2], ctrlPoints[3], ctrlPoints[4]);
        }

        var bezier2 = new List<fk_Vector> { ctrlPoints[3], q, ctrlPoints[4], ctrlPoints[5] };

        // 制御点を可視化するための赤い球を作成
        var qModel = new fk_Model();
        qModel.Shape = sphere;
        qModel.Material = fk_Material.Red;
        qModel.GlMoveTo(q);
        win.Entry(qModel);

        lineFlg = true;
    }

    if (lineFlg)
    {
        double t = (DateTime.Now.Millisecond % 1000) / 1000.0;
        var pos = GetBezierPoint(ctrlPoints.GetRange(0, 4), new List<fk_Vector> { ctrlPoints[3], CalcC1ContinuousPoint(ctrlPoints[2], ctrlPoints[3], ctrlPoints[4]), ctrlPoints[4], ctrlPoints[5] }, t);
        var tangent = GetBezierTangent(ctrlPoints.GetRange(0, 4), new List<fk_Vector> { ctrlPoints[3], CalcC1ContinuousPoint(ctrlPoints[2], ctrlPoints[3], ctrlPoints[4]), ctrlPoints[4], ctrlPoints[5] }, t);

        coneModel.GlMoveTo(pos);
        coneModel.GlVec(tangent);
    }
}
