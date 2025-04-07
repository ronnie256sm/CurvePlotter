import matplotlib.pyplot as plt
import numpy as np

def read_spline_output(filename):
    x_vals, y_vals = [], []
    with open(filename, 'r') as file:
        for line in file:
            x, y = map(float, line.split())
            x_vals.append(x)
            y_vals.append(y)
    return np.array(x_vals), np.array(y_vals)

def read_control_points(filename):
    x_vals, y_vals = [], []
    with open(filename, 'r') as file:
        num_points = int(file.readline().strip())
        for _ in range(num_points):
            x, y = map(float, file.readline().split())
            x_vals.append(x)
            y_vals.append(y)
    return np.array(x_vals), np.array(y_vals)

x_spline, y_spline = read_spline_output("interpolating_output.txt")
x_control, y_control = read_control_points("points.txt")

plt.figure(figsize=(10, 6))
plt.plot(x_spline, y_spline, label="Interpolating cubic spline", color="blue", linewidth=2)
#plt.scatter(x_control, y_control, label="Control points", color="red", marker="o", zorder=3)

plt.xlabel("x")
plt.ylabel("y")
plt.title("Interpolating Cubic Spline")
plt.legend()
plt.grid(True, linestyle="--", linewidth=0.5)

plt.show()
